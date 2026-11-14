export type Status = 'nominal' | 'watch' | 'urgent' | 'critical'

export interface Vitals {
  hr: number
  spo2: number
  temp: number
  sbp: number
  dbp: number
  rr: number
}

export interface Symptom {
  id: string
  sev: number
  ts: number
  src: 'manual' | 'auto' | 'sensor'
  note: string
}

export interface Risk {
  scores: Record<string, number>
  max: number
  top: string
  status: Status
}

export interface Crew {
  id: string
  name: string
  role: string
  color: string
  age: number
  vitals: Vitals
  sleep: number
  exercise: number
  eva: number
  dose: number
  co2: number
  symptoms: Symptom[]
  risk: Risk
  status: Status
  trend: number[]
  source: 'sim' | 'device'
  lastSeen: number
}

export interface Alert {
  id: string
  key: string
  crewId: string
  domain: string
  level: Status
  title: string
  msg: string
  ts: number
  ack: boolean
}

export interface Mission {
  name: string
  destination: string
  day: number
  minutes: number
  storm: boolean
}

export interface Stats {
  avgSleep: number
  maxDose: number
  activeAlerts: number
  ready: number
}

export interface SensorResult {
  crewId: string
  kind: 'vision' | 'voice' | 'typing' | 'mood'
  ts: number
  metrics: Record<string, number>
  flags: string[]
  level: Status
  note: string
}

export interface Snapshot {
  type: string
  ts: number
  mission: Mission
  crew: Crew[]
  alerts: Alert[]
  stats: Stats
  simulate: boolean
  auto: boolean
  commDelay: number
  sensors: SensorResult[]
  storage: string
}

export interface ReportRisk {
  id: string
  name: string
  status: Status
  top: string
  score: number
}

export interface ReportCrew {
  id: string
  name: string
  role: string
  status: Status
  vitals: Vitals
  sleep: number
  dose: number
  top: { name: string; score: number }[]
  symptoms: { label: string; sev: number; src: string }[]
  recs: string[]
}

export interface Report {
  id: string
  scope: string
  title: string
  ts: number
  day: number
  status: 'draft' | 'sent' | 'received' | 'ack'
  summary: {
    total: number
    atRisk: ReportRisk[]
    avgSleep: number
    maxDose: number
    alerts: number
  }
  crew: ReportCrew[]
  sentAt?: number
  eta?: number
  receivedAt?: number
  ackedAt?: number
  reply?: string
}
