import type { Kind } from './evaluate'

export interface LocalResult {
  id: string
  kind: Kind
  ts: number
  metrics: Record<string, number>
  flags: string[]
  level: string
  note: string
  synced: boolean
}

const KEY = 'orchest.sensors.v1'
const MAX = 300

export function loadResults(): LocalResult[] {
  try {
    const raw = localStorage.getItem(KEY)
    const arr = raw ? (JSON.parse(raw) as LocalResult[]) : []
    return Array.isArray(arr) ? arr : []
  } catch {
    return []
  }
}

export function saveResult(r: LocalResult) {
  try {
    const arr = loadResults()
    arr.unshift(r)
    localStorage.setItem(KEY, JSON.stringify(arr.slice(0, MAX)))
  } catch {
    /* storage full or blocked */
  }
}

export function markSynced(id: string) {
  try {
    const arr = loadResults().map((x) => (x.id === id ? { ...x, synced: true } : x))
    localStorage.setItem(KEY, JSON.stringify(arr))
  } catch {
    /* ignore */
  }
}

export function clearResults() {
  try {
    localStorage.removeItem(KEY)
  } catch {
    /* ignore */
  }
}

export async function trySync(r: LocalResult): Promise<boolean> {
  if (!navigator.onLine) return false
  try {
    const res = await fetch('/api/sensors', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ crewId: 'c1', kind: r.kind, metrics: r.metrics, note: r.note })
    })
    if (!res.ok) return false
    markSynced(r.id)
    return true
  } catch {
    return false
  }
}

export const newId = () => `l${Date.now()}${Math.floor(Math.random() * 1000)}`
