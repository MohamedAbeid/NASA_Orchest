import { useRef, useState } from 'react'
import type { SensorResult } from '../../types'

type Post = (kind: 'vision' | 'voice' | 'typing', metrics: Record<string, number>) => Promise<SensorResult | null>

const TARGET = 'Welcome to the Orchest health station, we monitor the crew health every second.'

export default function TypingCard({ post, notify }: {
  crewId: string
  post: Post
  notify: (m: string) => void
}) {
  const [typed, setTyped] = useState('')
  const [result, setResult] = useState<Record<string, number> | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [status, setStatus] = useState('')
  const presses = useRef(0)
  const wrong = useRef(0)
  const backs = useRef(0)
  const intervals = useRef<number[]>([])
  const lastKey = useRef(0)
  const startedAt = useRef(0)
  const doneRef = useRef(false)

  const reset = () => {
    setTyped('')
    setResult(null)
    presses.current = 0
    wrong.current = 0
    backs.current = 0
    intervals.current = []
    lastKey.current = 0
    startedAt.current = 0
    doneRef.current = false
    setStatus('')
  }

  const analyze = async (text = typed) => {
    const now = performance.now()
    const minutes = startedAt.current ? Math.max(0.02, (now - startedAt.current) / 60000) : 0.02
    let correct = 0
    for (let i = 0; i < Math.min(text.length, TARGET.length); i++) if (text[i] === TARGET[i]) correct++
    const total = Math.max(1, presses.current + backs.current)
    const mean = intervals.current.length ? intervals.current.reduce((a, b) => a + b, 0) / intervals.current.length : 0
    const sd = intervals.current.length > 1
      ? Math.sqrt(intervals.current.reduce((s, v) => s + (v - mean) ** 2, 0) / (intervals.current.length - 1))
      : 0
    const metrics: Record<string, number> = {
      wpm: Math.round((correct / 5) / minutes),
      errorRate: Math.round((wrong.current / total) * 100) / 100,
      backspaceRate: Math.round((backs.current / total) * 100) / 100,
      intervalCv: Math.round((mean ? sd / mean : 0) * 100) / 100,
      accuracy: Math.round((correct / Math.max(1, text.length)) * 100),
      chars: text.length
    }
    setResult(metrics)
    setSubmitting(true)
    try {
      const r = await post('typing', metrics)
      if (r) notify(r.flags.length ? `Analysis saved: ${r.flags.length} typing flags` : 'Analysis saved: typing looks normal')
    } finally {
      setSubmitting(false)
    }
  }

  const onChange = (v: string) => {
    if (doneRef.current) return
    if (!startedAt.current && v.length > 0) {
      startedAt.current = performance.now()
      lastKey.current = startedAt.current
    }
    if (v.length > typed.length) {
      for (let i = typed.length; i < v.length; i++) {
        presses.current++
        if (v[i] !== TARGET[i]) wrong.current++
      }
    } else if (v.length < typed.length) {
      backs.current += typed.length - v.length
    }
    setTyped(v)
    if (v === TARGET) {
      doneRef.current = true
      setStatus('Sentence complete — analysing')
      void analyze(v)
    }
  }

  const onKeyDown = () => {
    if (doneRef.current) return
    const now = performance.now()
    if (startedAt.current && now - lastKey.current > 0) intervals.current.push(now - lastKey.current)
    lastKey.current = now
  }

  const progress = Math.round((typed.length / TARGET.length) * 100)

  return (
    <div className="card sensor-card">
      <div className="hd">
        <div>
          <h2>Keyboard &amp; accuracy</h2>
          <p className="hint">Type the sentence exactly — errors, corrections and irregular rhythm can indicate tremor or motor/cognitive slowing.</p>
        </div>
        <button className="btn btn-ghost btn-sm" onClick={reset}>Reset</button>
      </div>

      <div className="type-target">{TARGET}</div>
      <textarea
        className="type-area"
        value={typed}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={onKeyDown}
        placeholder="Start typing here..."
        rows={3}
        dir="ltr"
        disabled={doneRef.current || submitting}
      />
      <div className="track" style={{ marginTop: 6 }}>
        <div className="fill" style={{ width: `${progress}%`, background: progress >= 100 ? '#4ade80' : '#3ddad7' }} />
      </div>
      <div className="voice-sub" style={{ marginTop: 4 }}>
        {status || `${progress}% complete · ${typed.length}/${TARGET.length} characters`}
      </div>

      {result && (
        <div className="metrics four">
          <Metric k="Speed" v={`${result.wpm} wpm`} warn={result.wpm < 15} />
          <Metric k="Error rate" v={`${Math.round(result.errorRate * 100)}%`} warn={result.errorRate > 0.12} />
          <Metric k="Corrections" v={`${Math.round(result.backspaceRate * 100)}%`} warn={result.backspaceRate > 0.25} />
          <Metric k="Rhythm variation" v={result.intervalCv.toFixed(2)} warn={result.intervalCv > 0.7} />
          <Metric k="Accuracy" v={`${result.accuracy}%`} />
        </div>
      )}

      <div className="row-btns" style={{ marginTop: 8 }}>
        <button className="btn btn-teal btn-sm" onClick={() => void analyze()} disabled={!typed || submitting || doneRef.current}>
          Analyse typing
        </button>
        <button className="btn btn-ghost btn-sm" disabled={submitting} onClick={() => {
          reset()
          const m = { wpm: 11, errorRate: 0.21, backspaceRate: 0.36, intervalCv: 0.94, accuracy: 78, chars: 40 }
          setResult(m)
          setSubmitting(true)
          post('typing', m).then((r) => r && notify(`Analysis saved: ${r.flags.length} typing flags`)).finally(() => setSubmitting(false))
        }}>Simulate tremor &amp; errors</button>
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
