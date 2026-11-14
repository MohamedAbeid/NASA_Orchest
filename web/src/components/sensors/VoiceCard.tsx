import { useEffect, useRef, useState } from 'react'
import type { SensorResult } from '../../types'

type Post = (kind: 'vision' | 'voice' | 'typing', metrics: Record<string, number>) => Promise<SensorResult | null>

const PROMPT = 'I am feeling fine today, but I got a little tired after the activity outside the vehicle, and I need to rest for a bit.'

function pitchAt(buf: Float32Array, sampleRate: number): number {
  const n = buf.length
  let rms = 0
  for (let i = 0; i < n; i++) rms += buf[i] * buf[i]
  rms = Math.sqrt(rms / n)
  if (rms < 0.01) return -1
  const c = new Float32Array(n)
  for (let lag = 0; lag < n; lag++) {
    let s = 0
    for (let i = 0; i < n - lag; i++) s += buf[i] * buf[i + lag]
    c[lag] = s
  }
  let d = 0
  while (d < n - 1 && c[d] > c[d + 1]) d++
  let max = -1
  let pos = -1
  for (let i = d; i < Math.floor(n / 2); i++) {
    if (c[i] > max) { max = c[i]; pos = i }
  }
  if (pos <= 0) return -1
  const t0 = c[pos - 1]
  const t1 = c[pos]
  const t2 = c[pos + 1]
  const denom = t0 - 2 * t1 + t2
  const shift = denom ? (t0 - t2) / (2 * denom) : 0
  const f = sampleRate / (pos + shift)
  return f > 60 && f < 450 ? f : -1
}

function wer(target: string, heard: string): number {
  const norm = (s: string) => s.replace(/[^a-z'\s]/gi, '').replace(/\s+/g, ' ').trim().split(' ')
  const a = norm(target)
  const b = norm(heard)
  if (!heard.trim()) return 1
  const dp = Array.from({ length: a.length + 1 }, (_, i) => [i, ...Array(b.length).fill(0)])
  for (let j = 1; j <= b.length; j++) dp[0][j] = j
  for (let i = 1; i <= a.length; i++)
    for (let j = 1; j <= b.length; j++)
      dp[i][j] = Math.min(dp[i - 1][j] + 1, dp[i][j - 1] + 1, dp[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1))
  return Math.min(1, dp[a.length][b.length] / a.length)
}

export default function VoiceCard({ post, notify }: {
  crewId: string
  post: Post
  notify: (m: string) => void
}) {
  const [rec, setRec] = useState(false)
  const [left, setLeft] = useState(8)
  const [level, setLevel] = useState(0)
  const [bars, setBars] = useState<number[]>([])
  const [status, setStatus] = useState('')
  const [transcript, setTranscript] = useState('')
  const [result, setResult] = useState<Record<string, number> | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const ctxRef = useRef<AudioContext | null>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const timerRef = useRef(0)
  const frames = useRef<{ rms: number; f0: number }[]>([])
  const recogRef = useRef<any>(null)
  const heardRef = useRef('')
  const stopRef = useRef<() => void>(() => {})

  useEffect(() => () => stopRef.current(), [])

  const stop = async () => {
    window.clearInterval(timerRef.current)
    const stream = streamRef.current
    streamRef.current = null
    setRec(false)
    try { recogRef.current?.stop() } catch { /* ignore */ }
    await ctxRef.current?.close()
    ctxRef.current = null
    stream?.getTracks().forEach((t) => t.stop())
    if (!frames.current.length) return
    const fs = frames.current
    const voiced = fs.filter((f) => f.f0 > 0)
    const rmsAvg = fs.reduce((s, f) => s + f.rms, 0) / fs.length
    const rmsDb = 20 * Math.log10(Math.max(1e-6, rmsAvg))
    const f0s = voiced.map((f) => f.f0)
    const mean = f0s.length ? f0s.reduce((a, b) => a + b, 0) / f0s.length : 0
    const sd = f0s.length > 1 ? Math.sqrt(f0s.reduce((s, f) => s + (f - mean) ** 2, 0) / (f0s.length - 1)) : 0
    const jitter = mean ? sd / mean : 0
    const pauseRatio = fs.filter((f) => f.rms < 0.01).length / fs.length
    let breaks = 0
    let run = 0
    for (const f of fs) {
      if (f.rms < 0.01) { run++; if (run === 6) breaks++ } else run = 0
    }
    const spoken = heardRef.current.trim()
    const w = spoken ? wer(PROMPT, spoken) : 0
    const metrics: Record<string, number> = {
      rmsDb: Math.round(rmsDb * 10) / 10,
      pitchHz: Math.round(mean),
      jitter: Math.round(jitter * 1000) / 1000,
      pauseRatio: Math.round(pauseRatio * 100) / 100,
      breaks,
      durationSec: Math.round(fs.length * 0.05),
      wordErrorRate: Math.round(w * 100) / 100
    }
    setResult(metrics)
    setSubmitting(true)
    try {
      const r = await post('voice', metrics)
      if (r) notify(r.flags.length ? `Analysis saved: ${r.flags.length} voice-change flags` : 'Analysis saved: voice looks normal')
    } finally {
      setSubmitting(false)
    }
  }
  stopRef.current = () => { void stop() }

  const start = async () => {
    setStatus('')
    setTranscript('')
    heardRef.current = ''
    frames.current = []
    setResult(null)
    setBars([])
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true } })
      streamRef.current = stream
      const ctx = new AudioContext()
      ctxRef.current = ctx
      const src = ctx.createMediaStreamSource(stream)
      const analyser = ctx.createAnalyser()
      analyser.fftSize = 2048
      src.connect(analyser)
      const buf = new Float32Array(analyser.fftSize)

      const SR = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition
      if (SR) {
        try {
          const r = new SR()
          r.lang = 'en-US'
          r.continuous = true
          r.interimResults = false
          r.onresult = (e: any) => {
            for (let i = e.resultIndex; i < e.results.length; i++)
              if (e.results[i].isFinal) heardRef.current += ' ' + e.results[i][0].transcript
            setTranscript(heardRef.current.trim())
          }
          r.onerror = () => { /* speech recognition is optional */ }
          r.start()
          recogRef.current = r
        } catch { recogRef.current = null }
      }

      setRec(true)
      setLeft(8)
      let ticks = 0
      timerRef.current = window.setInterval(() => {
        analyser.getFloatTimeDomainData(buf)
        let rms = 0
        for (let i = 0; i < buf.length; i++) rms += buf[i] * buf[i]
        rms = Math.sqrt(rms / buf.length)
        const f0 = pitchAt(buf, ctx.sampleRate)
        frames.current.push({ rms, f0 })
        setLevel(Math.min(1, rms * 8))
        setBars((b) => [...b.slice(-39), Math.min(1, rms * 8)])
        ticks++
        const remain = Math.max(0, 8 - ticks * 0.05)
        setLeft(remain)
        if (remain <= 0) void stop()
      }, 50)
    } catch {
      setStatus('Microphone unavailable — allow access from your browser.')
    }
  }

  return (
    <div className="card sensor-card">
      <div className="hd">
        <div>
          <h2>Microphone &amp; voice analysis</h2>
          <p className="hint">Record 8 seconds while reading the sentence — we measure loudness, pitch stability, pauses and speech clarity.</p>
        </div>
      </div>

      <div className="voice-prompt">«{PROMPT}»</div>

      <div className="voice-box">
        <button className={`mic ${rec ? 'on' : ''}`} onClick={() => (rec ? void stop() : void start())} aria-label="Record">
          {rec ? <span className="stop-ic" /> : <MicIcon />}
        </button>
        <div className="voice-live">
          <div className="bars">
            {Array.from({ length: 40 }).map((_, i) => (
              <i key={i} style={{ height: `${Math.round((bars[i] ?? 0) * 100)}%` }} />
            ))}
          </div>
          <div className="voice-sub">
            {rec ? `Recording... ${left.toFixed(1)} s` : result ? 'Analysis complete' : 'Tap the mic and read the sentence'}
            {transcript && <span> · Transcript: {transcript}</span>}
          </div>
        </div>
      </div>
      {status && <div className="sensor-status">{status}</div>}

      {result && (
        <div className="metrics four">
          <Metric k="Loudness" v={`${result.rmsDb.toFixed(0)} dB`} warn={result.rmsDb < -38} />
          <Metric k="Pitch" v={`${result.pitchHz} Hz`} />
          <Metric k="Pitch jitter" v={`${(result.jitter * 100).toFixed(1)}%`} warn={result.jitter > 0.05} />
          <Metric k="Pauses" v={`${Math.round(result.pauseRatio * 100)}%`} warn={result.pauseRatio > 0.45} />
          <Metric k="Voice dropouts" v={`${result.breaks} times`} warn={result.breaks >= 4} />
          <Metric k="Speech clarity" v={transcript ? `${Math.round(result.wordErrorRate * 100)}% error` : '—'} warn={result.wordErrorRate > 0.35} />
        </div>
      )}

      <div className="row-btns" style={{ marginTop: 8 }}>
        <button className="btn btn-ghost btn-sm" disabled={rec || submitting} onClick={() => {
          const m = { rmsDb: -44, pitchHz: 110, jitter: 0.062, pauseRatio: 0.46, breaks: 5, durationSec: 8, wordErrorRate: 0.55 }
          setResult(m)
          setSubmitting(true)
          post('voice', m).then((r) => r && notify(`Analysis saved: ${r.flags.length} voice-change flags`)).finally(() => setSubmitting(false))
        }}>Simulate hoarse, weak voice</button>
        <span className="hint">Recording happens locally in your browser.</span>
      </div>
    </div>
  )
}

function Metric({ k, v, warn }: { k: string; v: string; warn?: boolean }) {
  return (
    <div className={`metric ${warn ? 'warn' : ''}`}>
      <span>{k}</span><b>{v}</b>
    </div>
  )
}

function MicIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" width="22" height="22">
      <rect x="9" y="3" width="6" height="11" rx="3" /><path d="M5 11a7 7 0 0 0 14 0M12 18v3" />
    </svg>
  )
}
