import { useCallback, useEffect, useRef, useState } from 'react'
import { FilesetResolver, FaceLandmarker } from '@mediapipe/tasks-vision'
import type { SensorResult } from '../../types'
import type { Kind } from '../../sensors/evaluate'

type Post = (kind: Kind, metrics: Record<string, number>) => Promise<SensorResult>

interface Mood {
  seconds: number
  frown: number
  brow: number
  smile: number
  gazeDown: number
  headDown: number
  flatAffect: number
  score: number
}

interface Live {
  face: boolean
  distanceCm: number
  blinkRate: number
  squint: number
  gazeOff: number
  mood: Mood
}

const EMPTY_MOOD: Mood = { seconds: 0, frown: 0, brow: 0, smile: 0, gazeDown: 0, headDown: 0, flatAffect: 0, score: 0 }
const FACES = [33, 133, 362, 263, 468, 473, 1, 4, 61, 291, 13, 14, 10, 152]

const clamp01 = (v: number) => Math.max(0, Math.min(1, v))
const std = (a: number[]) => {
  if (a.length < 2) return 0
  const m = a.reduce((x, y) => x + y, 0) / a.length
  return Math.sqrt(a.reduce((s, v) => s + (v - m) ** 2, 0) / a.length)
}

function moodScore(m: Omit<Mood, 'score'>): number {
  let s = 0
  if (m.frown > 0.15) s += 18
  if (m.brow > 0.25) s += 14
  if (m.seconds > 15 && m.smile < 0.08) s += 14
  if (m.gazeDown > 0.4) s += 12
  if (m.headDown > 0.5) s += 10
  if (m.flatAffect > 0.65 && m.seconds > 20) s += 14
  return Math.min(100, Math.round(s))
}

export default function VisionCard({ post, notify, focusMood }: {
  post: Post
  notify: (m: string) => void
  focusMood?: boolean
}) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const lmRef = useRef<FaceLandmarker | null>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const rafRef = useRef(0)
  const lastTime = useRef(-1)
  const blinkRef = useRef<number[]>([])
  const lastBlink = useRef(0)
  const gazeRef = useRef({ off: 0, total: 0 })
  const moodRef = useRef({ n: 0, frown: 0, brow: 0, smile: 0, gazeDown: 0, headDown: 0, start: 0, hist: [] as number[] })
  const liveRef = useRef<Live>({ face: false, distanceCm: 0, blinkRate: 0, squint: 0, gazeOff: 0, mood: EMPTY_MOOD })

  const [camOn, setCamOn] = useState(false)
  const [status, setStatus] = useState('')
  const [live, setLive] = useState<Live>(liveRef.current)
  const [zoom, setZoom] = useState(1)
  const [fontPx, setFontPx] = useState(16)
  const [auto, setAuto] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const baseDpr = useRef(1)
  const baseW = useRef(1)

  useEffect(() => {
    baseDpr.current = window.devicePixelRatio || 1
    baseW.current = window.innerWidth
    const onResize = () => {
      const z = Math.max(
        (window.devicePixelRatio || 1) / baseDpr.current,
        baseW.current / Math.max(1, window.innerWidth)
      )
      setZoom(Math.round(Math.min(3, Math.max(1, z)) * 100) / 100)
    }
    window.addEventListener('resize', onResize)
    window.visualViewport?.addEventListener('resize', onResize)
    return () => {
      window.removeEventListener('resize', onResize)
      window.visualViewport?.removeEventListener('resize', onResize)
    }
  }, [])

  const resetMood = () => {
    moodRef.current = { n: 0, frown: 0, brow: 0, smile: 0, gazeDown: 0, headDown: 0, start: 0, hist: [] }
  }

  const stopCamera = useCallback(() => {
    cancelAnimationFrame(rafRef.current)
    streamRef.current?.getTracks().forEach((t) => t.stop())
    streamRef.current = null
    lmRef.current?.close()
    lmRef.current = null
    setCamOn(false)
    setStatus('')
  }, [])

  useEffect(() => () => stopCamera(), [stopCamera])

  const startCamera = async () => {
    setStatus('Loading face tracking model...')
    try {
      const fileset = await FilesetResolver.forVisionTasks('/mediapipe/wasm')
      let lm: FaceLandmarker
      try {
        lm = await FaceLandmarker.createFromOptions(fileset, {
          baseOptions: { modelAssetPath: '/models/face_landmarker.task', delegate: 'GPU' },
          runningMode: 'VIDEO', numFaces: 1, outputFaceBlendshapes: true
        })
      } catch {
        lm = await FaceLandmarker.createFromOptions(fileset, {
          baseOptions: { modelAssetPath: '/models/face_landmarker.task', delegate: 'CPU' },
          runningMode: 'VIDEO', numFaces: 1, outputFaceBlendshapes: true
        })
      }
      lmRef.current = lm
      setStatus('Starting the camera...')
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { width: { ideal: 640 }, height: { ideal: 480 }, facingMode: 'user' }, audio: false
      })
      streamRef.current = stream
      const video = videoRef.current!
      video.srcObject = stream
      await video.play()
      setCamOn(true)
      setStatus('')
      blinkRef.current = []
      gazeRef.current = { off: 0, total: 0 }
      resetMood()
      lastTime.current = -1

      const canvas = canvasRef.current!
      const ctx = canvas.getContext('2d')!
      const loop = () => {
        rafRef.current = requestAnimationFrame(loop)
        if (!videoRef.current || videoRef.current.readyState < 2) return
        if (videoRef.current.currentTime === lastTime.current) return
        lastTime.current = videoRef.current.currentTime
        const res = lmRef.current?.detectForVideo(videoRef.current, performance.now())
        const w = videoRef.current.videoWidth
        const h = videoRef.current.videoHeight
        if (canvas.width !== w) { canvas.width = w; canvas.height = h }
        ctx.save()
        ctx.setTransform(1, 0, 0, 1, 0, 0)
        ctx.clearRect(0, 0, w, h)
        ctx.translate(w, 0)
        ctx.scale(-1, 1)
        const face = res?.faceLandmarks?.[0]
        const now = performance.now()
        if (face) {
          const d = Math.abs(face[33].x - face[263].x) * w
          const distanceCm = d > 0 ? (0.9 * w * 6.3) / d : 0
          const bs = res?.faceBlendshapes?.[0]?.categories ?? []
          const sc = (n: string) => bs.find((c) => c.categoryName === n)?.score ?? 0
          const blink = (sc('eyeBlinkLeft') + sc('eyeBlinkRight')) / 2
          if (blink > 0.5 && now - lastBlink.current > 350) {
            lastBlink.current = now
            blinkRef.current.push(now)
          }
          blinkRef.current = blinkRef.current.filter((t) => now - t < 60000)
          const squint = (sc('eyeSquintLeft') + sc('eyeSquintRight')) / 2
          const lr = (face[468].x - face[33].x) / Math.max(1e-6, face[133].x - face[33].x)
          const rr = (face[473].x - face[362].x) / Math.max(1e-6, face[263].x - face[362].x)
          const gaze = (lr + rr) / 2
          const off = Math.abs(gaze - 0.5) > 0.22
          gazeRef.current.total++
          if (off) gazeRef.current.off++

          // ---- mood indicators from facial expression ----
          const m = moodRef.current
          if (!m.start) m.start = now
          const frown = (sc('mouthFrownLeft') + sc('mouthFrownRight')) / 2
          const brow = (sc('browDownLeft') + sc('browDownRight') + sc('browInnerUp')) / 3
          const smile = (sc('mouthSmileLeft') + sc('mouthSmileRight')) / 2
          const lookDown = (sc('eyeLookDownLeft') + sc('eyeLookDownRight')) / 2
          const faceLen = Math.max(1e-6, face[152].y - face[10].y)
          const noseRel = (face[4].y - (face[33].y + face[263].y) / 2) / faceLen
          const headDown = noseRel > 0.22 ? 1 : 0
          m.n++
          m.frown += frown
          m.brow += brow
          m.smile += smile
          if (lookDown > 0.25) m.gazeDown++
          if (headDown) m.headDown++
          m.hist.push(smile)
          if (m.hist.length > 900) m.hist.shift()
          const seconds = (now - m.start) / 1000
          const avg = {
            frown: m.frown / m.n,
            brow: m.brow / m.n,
            smile: m.smile / m.n,
            gazeDown: m.gazeDown / m.n,
            headDown: m.headDown / m.n,
            flatAffect: clamp01(1 - std(m.hist) * 25),
            seconds
          }
          const mood: Mood = { ...avg, score: seconds >= 5 ? moodScore(avg) : 0 }

          liveRef.current = {
            face: true,
            distanceCm: Math.round(distanceCm),
            blinkRate: blinkRef.current.length,
            squint: Math.round(squint * 100) / 100,
            gazeOff: gazeRef.current.total ? Math.round((gazeRef.current.off / gazeRef.current.total) * 100) / 100 : 0,
            mood
          }
          for (const i of FACES) {
            ctx.beginPath()
            ctx.arc(face[i].x * w, face[i].y * h, i >= 468 ? 4 : 3, 0, Math.PI * 2)
            ctx.fillStyle = i >= 468 ? '#ff8a3d' : '#3ddad7'
            ctx.fill()
          }
          ctx.strokeStyle = 'rgba(255,138,61,.75)'
          ctx.lineWidth = 2
          const xs = face.map((p) => p.x * w)
          const ys = face.map((p) => p.y * h)
          ctx.strokeRect(Math.min(...xs) - 6, Math.min(...ys) - 6, Math.max(...xs) - Math.min(...xs) + 12, Math.max(...ys) - Math.min(...ys) + 12)
        } else {
          liveRef.current = { face: false, distanceCm: 0, blinkRate: 0, squint: 0, gazeOff: 0, mood: EMPTY_MOOD }
        }
        ctx.restore()
      }
      loop()
    } catch (e) {
      setStatus(e instanceof Error && e.name === 'NotAllowedError'
        ? 'Camera permission denied — allow access from your browser settings.'
        : 'Could not start the camera or the face tracking model.')
    }
  }

  useEffect(() => {
    const t = window.setInterval(() => setLive(liveRef.current), 700)
    return () => window.clearInterval(t)
  }, [])

  const metrics = useCallback((): Record<string, number> => ({
    zoom, fontPx,
    distanceCm: liveRef.current.distanceCm,
    blinkRate: liveRef.current.blinkRate,
    squint: liveRef.current.squint,
    gazeOff: liveRef.current.gazeOff,
    face: liveRef.current.face ? 1 : 0
  }), [zoom, fontPx])

  const moodMetrics = useCallback((): Record<string, number> => {
    const m = liveRef.current.mood
    return {
      depressionScore: m.score,
      frown: Math.round(m.frown * 1000) / 1000,
      brow: Math.round(m.brow * 1000) / 1000,
      smile: Math.round(m.smile * 1000) / 1000,
      gazeDown: Math.round(m.gazeDown * 100) / 100,
      headDown: Math.round(m.headDown * 100) / 100,
      flatAffect: Math.round(m.flatAffect * 100) / 100,
      blinkRate: liveRef.current.blinkRate,
      seconds: Math.round(m.seconds),
      face: 1
    }
  }, [])

  const submit = async (m?: Record<string, number>) => {
    setSubmitting(true)
    try {
      const r = await post('vision', m ?? metrics())
      if (r) notify(r.flags.length ? `Analysis saved: ${r.flags.length} visual strain flags` : 'Analysis saved: reading looks normal')
    } finally {
      setSubmitting(false)
    }
  }

  const submitMood = async (m?: Record<string, number>) => {
    setSubmitting(true)
    try {
      const r = await post('mood', m ?? moodMetrics())
      if (r) {
        const s = r.metrics.depressionScore ?? 0
        notify(r.flags.length ? `Mood score ${s}/100 — ${r.flags.length} indicators` : 'Mood indicators look normal')
      }
    } finally {
      setSubmitting(false)
    }
  }

  useEffect(() => {
    if (!auto) return
    const t = window.setInterval(() => { submit() }, 30000)
    return () => window.clearInterval(t)
  }, [auto]) // eslint-disable-line react-hooks/exhaustive-deps

  const strain = Math.min(100, Math.round(
    (zoom >= 1.25 ? 30 : zoom >= 1.1 ? 12 : 0) +
    (fontPx >= 20 ? 25 : fontPx >= 18 ? 10 : 0) +
    (live.distanceCm > 0 && live.distanceCm < 35 ? 25 : 0) +
    (live.blinkRate > 0 && live.blinkRate < 10 ? 20 : 0) +
    (live.squint >= 0.35 ? 15 : 0) +
    (live.gazeOff >= 0.4 ? 10 : 0)
  ))

  const mood = live.mood
  const moodReady = mood.seconds >= 15
  const moodCol = mood.score >= 60 ? '#ff5d6c' : mood.score >= 35 ? '#ffc857' : '#4ade80'

  return (
    <div className="card sensor-card wide">
      <div className="hd">
        <div>
          <h2>Face, gaze &amp; mood camera</h2>
          <p className="hint">Tracks face, blinking, distance and zoom (visual strain / SANS) plus facial expressions (mood and depression indicators).</p>
        </div>
        <div className="row-btns">
          {!camOn
            ? <button className="btn btn-primary btn-sm" onClick={startCamera}>Start camera</button>
            : <button className="btn btn-ghost btn-sm" onClick={stopCamera}>Stop</button>}
        </div>
      </div>

      <div className="sensor-body">
        <div className="cam-wrap">
          <video ref={videoRef} muted playsInline className={camOn ? 'on' : ''} />
          <canvas ref={canvasRef} className={camOn ? 'on' : ''} />
          {!camOn && <div className="cam-off">Camera is off<br /><span>Analysis runs on your device; no video is uploaded</span></div>}
          {camOn && <span className={`cam-tag ${live.face ? 'ok' : ''}`}>{live.face ? 'Face detected' : 'No face'}</span>}
        </div>

        <div>
          {status && <div className="sensor-status">{status}</div>}
          <div className="metrics">
            <Metric k="Approx. distance" v={live.distanceCm ? `${live.distanceCm} cm` : '—'} warn={live.distanceCm > 0 && live.distanceCm < 35} />
            <Metric k="Blinks / min" v={live.face ? String(live.blinkRate) : '—'} warn={live.face && live.blinkRate < 10} />
            <Metric k="Eye squint" v={live.face ? live.squint.toFixed(2) : '—'} warn={live.squint >= 0.35} />
            <Metric k="Gaze drift" v={live.face ? `${Math.round(live.gazeOff * 100)}%` : '—'} warn={live.gazeOff >= 0.4} />
            <Metric k="Browser zoom" v={`${zoom.toFixed(2)}x`} warn={zoom >= 1.25} />
            <Metric k="Reading font size" v={`${fontPx}px`} warn={fontPx >= 20} />
          </div>

          <div className="strain">
            <div className="l"><span>Reading strain index</span><b style={{ color: strain >= 60 ? '#ff5d6c' : strain >= 35 ? '#ffc857' : '#4ade80' }}>{strain}%</b></div>
            <div className="track"><div className="fill" style={{ width: `${strain}%`, background: strain >= 60 ? '#ff5d6c' : strain >= 35 ? '#ffc857' : '#4ade80' }} /></div>
          </div>

          <div className="reading">
            <div className="reading-top">
              <span>Reading test</span>
              <span className="fontctl">
                <button title="Increase font" onClick={() => setFontPx((f) => Math.min(32, f + 2))}>A+</button>
                <b>{fontPx}px</b>
                <button title="Decrease font" onClick={() => setFontPx((f) => Math.max(14, f - 2))}>A−</button>
              </span>
            </div>
            <p style={{ fontSize: fontPx }}>Read this sentence quietly and keep a comfortable distance from the screen. If you need to enlarge the text more than once, that suggests your eyes tire quickly.</p>
            {fontPx >= 20 && <div className="flag-inline">Font enlarged to {fontPx}px — a sign of reading difficulty</div>}
          </div>

          <div className="row-btns" style={{ marginTop: 10 }}>
            <button className="btn btn-teal btn-sm" onClick={() => submit()} disabled={submitting}>Analyse vision &amp; save</button>
            <label className="switch">
              <input type="checkbox" checked={auto} onChange={(e) => setAuto(e.target.checked)} />
              Auto-analyse every 30 s
            </label>
            <button className="btn btn-ghost btn-sm" onClick={() => submit({
              zoom: 1.5, fontPx: 26, distanceCm: 28, blinkRate: 6, squint: 0.45, gazeOff: 0.5, face: 1
            })} disabled={submitting}>Simulate visual strain</button>
          </div>
        </div>
      </div>

      <div className={`mood ${focusMood ? 'focus' : ''}`}>
        <div className="mood-hd">
          <div>
            <h3>Depression indicators from face tracking</h3>
            <p className="hint">From facial expressions (frown, tense brows, smiling, downward gaze, flat affect) during the camera session.</p>
          </div>
          <div className="mood-score" dir="ltr" style={{ color: live.face && mood.seconds >= 5 ? moodCol : '#6c7f9f' }}>
            {live.face && mood.seconds >= 5 ? mood.score : '—'}<span> / 100</span>
          </div>
        </div>
        <div className="track big">
          <div className="fill" style={{ width: `${live.face ? mood.score : 0}%`, background: moodCol }} />
        </div>
        <div className="metrics four">
          <Metric k="Frown" v={live.face ? mood.frown.toFixed(2) : '—'} warn={mood.frown > 0.15} />
          <Metric k="Brow tension" v={live.face ? mood.brow.toFixed(2) : '—'} warn={mood.brow > 0.25} />
          <Metric k="Smile" v={live.face ? mood.smile.toFixed(2) : '—'} warn={live.face && mood.seconds > 15 && mood.smile < 0.08} />
          <Metric k="Downward gaze" v={live.face ? `${Math.round(mood.gazeDown * 100)}%` : '—'} warn={mood.gazeDown > 0.4} />
          <Metric k="Flat affect" v={live.face ? mood.flatAffect.toFixed(2) : '—'} warn={mood.flatAffect > 0.65} />
          <Metric k="Session time" v={live.face ? `${Math.round(mood.seconds)} s` : '—'} />
        </div>
        <div className="row-btns" style={{ marginTop: 10 }}>
          <button className="btn btn-primary btn-sm" onClick={() => submitMood()} disabled={submitting || !moodReady}>
            {moodReady ? 'Analyse mood & save result' : `Need ${Math.max(0, 15 - Math.round(mood.seconds))} s more on camera`}
          </button>
          <button className="btn btn-ghost btn-sm" onClick={() => submitMood({
            depressionScore: 72, frown: 0.31, brow: 0.36, smile: 0.02, gazeDown: 0.55,
            headDown: 0.7, flatAffect: 0.82, blinkRate: 6, seconds: 40, face: 1
          })} disabled={submitting}>Simulate depression indicators</button>
          <button className="btn btn-ghost btn-sm" onClick={() => submitMood({
            depressionScore: 8, frown: 0.04, brow: 0.1, smile: 0.42, gazeDown: 0.1,
            headDown: 0.1, flatAffect: 0.1, blinkRate: 16, seconds: 40, face: 1
          })} disabled={submitting}>Simulate normal mood</button>
        </div>
        <p className="hint" style={{ marginTop: 8 }}>
          This score is a research signal based on facial expression, not a medical diagnosis. If it stays high across
          sessions, a PHQ-9 questionnaire and a clinician are the right next step.
        </p>
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
