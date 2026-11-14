import { useState } from 'react'
import type { SensorResult } from '../types'
import type { Kind } from '../sensors/evaluate'
import type { LocalResult } from '../sensors/store'
import VisionCard from './sensors/VisionCard'
import VoiceCard from './sensors/VoiceCard'
import TypingCard from './sensors/TypingCard'

export const KIND_LABEL: Record<Kind, string> = {
  vision: 'Vision & Gaze Camera',
  voice: 'Voice Analysis',
  typing: 'Typing Analysis',
  mood: 'Mood & Depression Indicators'
}

export const KIND_ICON: Record<Kind, string> = { vision: '◉', voice: '◍', typing: '⌨', mood: '☺' }

type Tab = 'all' | Kind

export default function SensorStation({ results, post, notify }: {
  results: LocalResult[]
  post: (crewId: string, kind: Kind, metrics: Record<string, number>) => Promise<SensorResult>
  notify: (m: string) => void
}) {
  const [tab, setTab] = useState<Tab>('all')
  const send = (kind: Kind, metrics: Record<string, number>) => post('local', kind, metrics)
  const mood = results.find((r) => r.kind === 'mood')
  const vision = results.find((r) => r.kind === 'vision')
  const voice = results.find((r) => r.kind === 'voice')
  const typing = results.find((r) => r.kind === 'typing')
  const dayCount = results.filter((r) => Date.now() - r.ts < 86400_000).length
  const flagged = results.filter((r) => r.flags.length > 0 && Date.now() - r.ts < 86400_000).length

  return (
    <div className="sensors-view">
      <div className="card">
        <div className="hd">
          <div>
            <h2>Sensor Station — everything in one place</h2>
            <p className="hint">
              The camera watches your face, gaze and mood expressions, the mic analyses your voice, and the keyboard
              analyses your typing. Everything is processed on your device and stored locally — no video or audio ever
              leaves it.
            </p>
            <span className={`db-pill ${flagged ? 'warn' : 'ok'}`}>
              {flagged ? `${flagged} indicators need attention in the last 24 hours` : 'No concerning indicators in the last 24 hours'}
            </span>
          </div>
          <div className="summary">
            <div><span>Last 24 hours</span><b>{dayCount} readings</b></div>
            <div><span>Last mood score</span><b className={scoreColor(mood ? (mood.metrics.depressionScore ?? 0) : -1)}>{mood ? Math.round(mood.metrics.depressionScore ?? 0) : '—'}</b></div>
            <div><span>Vision flags</span><b className={vision?.level === 'nominal' ? 'good' : vision ? 'warn' : ''}>{vision ? `${vision.flags.length}` : '—'}</b></div>
          </div>
        </div>
        <div className="sensor-tabs">
          {(['all', 'vision', 'mood', 'voice', 'typing'] as const).map((t) => (
            <button key={t} className={tab === t ? 'on' : ''} onClick={() => setTab(t)}>
              {t === 'all' ? 'All tools' : `${KIND_ICON[t]} ${KIND_LABEL[t]}`}
            </button>
          ))}
        </div>
      </div>

      <div className="sensor-grid">
        {(tab === 'all' || tab === 'vision' || tab === 'mood') && (
          <VisionCard post={send} notify={notify} focusMood={tab === 'mood'} />
        )}
        {(tab === 'all' || tab === 'voice') && (
          <VoiceCard crewId="local" post={send} notify={notify} />
        )}
        {(tab === 'all' || tab === 'typing') && (
          <TypingCard crewId="local" post={send} notify={notify} />
        )}
      </div>

      <div className="grid2">
        <div className="card">
          <h2>Latest results</h2>
          <p className="hint">The most recent reading from each tool — the full list is in the history below.</p>
          {!mood && !vision && !voice && !typing && (
            <div className="empty">No measurements yet — try a tool above or one of the simulation buttons.</div>
          )}
          <ul className="sensor-list">
            {[mood, vision, voice, typing].filter(Boolean).map((r) => (
              <li key={r!.id} className={r!.level}>
                <div className="sl-top">
                  <b>{KIND_ICON[r!.kind]} {KIND_LABEL[r!.kind]}</b>
                  <span>{new Date(r!.ts).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}</span>
                </div>
                <div className="sl-note">{r!.note}</div>
              </li>
            ))}
          </ul>
        </div>

        <div className="card">
          <h2>How measurements map to indicators</h2>
          <p className="hint">Grounded in NASA HRP risk domains: eyes &amp; brain (SANS), mood &amp; behaviour, respiratory and musculoskeletal.</p>
          <div className="map">
            <div><span className="mk" style={{ background: '#c084fc' }} />Font zoom / leaning into the screen</div>
            <div className="arr">→ visual strain and vision change (SANS)</div>
            <div><span className="mk" style={{ background: '#ff8a3d' }} />Frowning, tense brows, little smiling</div>
            <div className="arr">→ mood and depression indicators (Behavioral)</div>
            <div><span className="mk" style={{ background: '#38bdf8' }} />Weak, hoarse or broken voice</div>
            <div className="arr">→ respiratory + mood &amp; sleep</div>
            <div><span className="mk" style={{ background: '#fbbf24' }} />Typing errors / tremor</div>
            <div className="arr">→ musculoskeletal + eyes &amp; brain</div>
          </div>
          <p className="hint" style={{ marginTop: 10 }}>
            Note: depression detection from facial expression is an early research signal, not a replacement for a
            clinician or the PHQ-9 questionnaire.
          </p>
        </div>
      </div>
    </div>
  )
}

function scoreColor(s: number) {
  if (s < 0) return ''
  return s >= 60 ? 'bad' : s >= 35 ? 'warn' : 'good'
}
