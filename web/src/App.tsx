import { useCallback, useEffect, useRef, useState } from 'react'
import type { SensorResult, Status } from './types'
import type { Kind } from './sensors/evaluate'
import { evaluate } from './sensors/evaluate'
import { clearResults, loadResults, newId, saveResult, trySync } from './sensors/store'
import type { LocalResult } from './sensors/store'
import SensorStation, { KIND_ICON, KIND_LABEL } from './components/SensorStation'

type BipEvent = Event & { prompt: () => Promise<void>; userChoice: Promise<{ outcome: string }> }

export default function App() {
  const [online, setOnline] = useState(navigator.onLine)
  const [apiOk, setApiOk] = useState(false)
  const [results, setResults] = useState<LocalResult[]>(() => loadResults())
  const [toast, setToast] = useState('')
  const [installEvt, setInstallEvt] = useState<BipEvent | null>(null)
  const [installed, setInstalled] = useState(
    window.matchMedia?.('(display-mode: standalone)').matches || (navigator as unknown as { standalone?: boolean }).standalone === true
  )
  const toastTimer = useRef<number | undefined>(undefined)

  const showToast = useCallback((msg: string) => {
    setToast(msg)
    window.clearTimeout(toastTimer.current)
    toastTimer.current = window.setTimeout(() => setToast(''), 2800)
  }, [])

  useEffect(() => {
    const isLocal = ['localhost', '127.0.0.1'].includes(location.hostname)
    const probe = () => {
      if (!isLocal) return
      fetch('/api/health', { cache: 'no-store' })
        .then((r) => setApiOk(r.ok))
        .catch(() => setApiOk(false))
    }
    probe()
    const t = window.setInterval(probe, 60000)
    const on = () => setOnline(true)
    const off = () => setOnline(false)
    const bip = (e: Event) => {
      e.preventDefault()
      setInstallEvt(e as BipEvent)
    }
    const installedFn = () => setInstalled(true)
    window.addEventListener('online', on)
    window.addEventListener('offline', off)
    window.addEventListener('beforeinstallprompt', bip)
    window.addEventListener('appinstalled', installedFn)
    return () => {
      window.clearInterval(t)
      window.removeEventListener('online', on)
      window.removeEventListener('offline', off)
      window.removeEventListener('beforeinstallprompt', bip)
      window.removeEventListener('appinstalled', installedFn)
      window.clearTimeout(toastTimer.current)
    }
  }, [])

  useEffect(() => {
    if (!navigator.onLine) return
    const t = window.setTimeout(() => {
      fetch('/models/face_landmarker.task').catch(() => {})
      fetch('/mediapipe/wasm/vision_wasm_internal.wasm').catch(() => {})
    }, 5000)
    return () => window.clearTimeout(t)
  }, [])

  const post = useCallback(async (_crewId: string, kind: Kind, metrics: Record<string, number>): Promise<SensorResult> => {
    const out = evaluate(kind, metrics)
    const r: LocalResult = {
      id: newId(), kind, ts: Date.now(), metrics,
      flags: out.flags, level: out.level, note: out.note, synced: false
    }
    saveResult(r)
    setResults(loadResults())
    if (navigator.onLine && apiOk) {
      trySync(r).then((ok) => { if (ok) setResults(loadResults()) })
    }
    return {
      crewId: _crewId, kind, ts: r.ts, metrics, flags: r.flags,
      level: r.level as Status, note: r.note
    }
  }, [apiOk])

  const syncAll = async () => {
    const pending = loadResults().filter((r) => !r.synced)
    if (!pending.length) return showToast('All measurements are already synced')
    if (!navigator.onLine) return showToast('No internet — everything stays on this device')
    if (!apiOk) return showToast('Server not available — measurements stay on this device')
    let ok = 0
    for (const r of pending) if (await trySync(r)) ok++
    setResults(loadResults())
    showToast(ok ? `Synced ${ok} measurements` : 'Server unavailable — measurements stay on this device')
  }

  const install = async () => {
    if (!installEvt) return
    await installEvt.prompt()
    const c = await installEvt.userChoice
    if (c.outcome === 'accepted') setInstalled(true)
    setInstallEvt(null)
  }

  const synced = results.filter((r) => r.synced).length

  return (
    <div className="shell">
      <header className="shead">
        <div className="brand">
          <div className="logo">O</div>
          <div>
            <h1>Orchest</h1>
            <p>Health Sensor Station · Eyes, Ears, Hands &amp; Mood</p>
          </div>
        </div>
        <div className="spills">
          <span className={`pill ${online ? 'ok' : 'off'}`}>
            <span className="dot" />
            {online ? 'Online' : 'Offline — still working'}
          </span>
          <span className="pill">{results.length} saved</span>
          {installEvt && !installed && (
            <button className="btn btn-primary btn-sm" onClick={install}>Install app</button>
          )}
          {installed && <span className="pill ok">Installed</span>}
        </div>
      </header>

      <SensorStation results={results} post={post} notify={showToast} />

      <section className="card">
        <div className="hd">
          <div>
            <h2>Measurement history on this device</h2>
            <p className="hint">Every measurement is saved locally in the browser (localStorage) and works without internet. When a server is reachable you can sync it.</p>
          </div>
          <div className="row-btns">
            <button className="btn btn-ghost btn-sm" onClick={syncAll} disabled={!results.length}>
              Sync with server {synced ? `(${synced}/${results.length})` : ''}
            </button>
            <button
              className="btn btn-ghost btn-sm"
              onClick={() => { clearResults(); setResults([]); showToast('Local history cleared') }}
              disabled={!results.length}
            >
              Clear history
            </button>
          </div>
        </div>
        {results.length === 0 && <div className="empty">No measurements yet — start with the camera, the mic, or the typing test.</div>}
        <ul className="sensor-list">
          {results.slice(0, 40).map((r) => (
            <li key={r.id} className={r.level}>
              <div className="sl-top">
                <b>{KIND_ICON[r.kind]} {KIND_LABEL[r.kind]}</b>
                <span>{new Date(r.ts).toLocaleString('en-GB', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' })} · {r.synced ? 'synced' : 'local'}</span>
              </div>
              <div className="sl-note">{r.note}</div>
            </li>
          ))}
        </ul>
      </section>

      <footer className="sfoot">
        Everything is analysed on your device; no video or audio ever leaves it. Facial and mood indicators are a research signal, not a medical diagnosis.
      </footer>

      <div className={`toast ${toast ? 'on' : ''}`}>{toast}</div>
    </div>
  )
}
