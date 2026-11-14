export type Kind = 'vision' | 'voice' | 'typing' | 'mood'

export interface SensorOutcome {
  flags: string[]
  level: 'nominal' | 'watch' | 'urgent'
  note: string
}

const num = (m: Record<string, number>, k: string) => m[k] ?? 0

export function evaluate(kind: Kind, metrics: Record<string, number>): SensorOutcome {
  const flags: string[] = []
  const M = (k: string) => num(metrics, k)
  let label = ''

  if (kind === 'vision') {
    label = 'Visual strain'
    if (M('zoom') >= 1.25) flags.push(`Browser zoom at ${M('zoom').toFixed(2)}x`)
    if (M('fontPx') >= 20) flags.push(`Reading font enlarged to ${M('fontPx').toFixed(0)}px`)
    if (M('distanceCm') > 0 && M('distanceCm') < 35) flags.push(`Leaning close to the screen (${M('distanceCm').toFixed(0)} cm)`)
    if (M('blinkRate') > 0 && M('blinkRate') < 10) flags.push(`Low blink rate (${M('blinkRate').toFixed(0)}/min)`)
    if (M('squint') >= 0.35) flags.push('Frequent squinting')
    if (M('gazeOff') >= 0.4) flags.push('Frequent gaze drift')
  } else if (kind === 'voice') {
    label = 'Voice change'
    if (M('rmsDb') < -38) flags.push(`Weak voice (${M('rmsDb').toFixed(0)} dB)`)
    if (M('jitter') > 0.05) flags.push(`Pitch instability (${(M('jitter') * 100).toFixed(1)}%)`)
    if (M('pauseRatio') > 0.45) flags.push(`Many pauses (${(M('pauseRatio') * 100).toFixed(0)}%)`)
    if (M('wordErrorRate') > 0.35) flags.push(`Unclear speech (${(M('wordErrorRate') * 100).toFixed(0)}%)`)
    if (M('breaks') >= 4) flags.push(`Voice dropped out ${M('breaks').toFixed(0)} times`)
  } else if (kind === 'typing') {
    label = 'Typing errors'
    if (M('errorRate') > 0.12) flags.push(`Typing errors at ${(M('errorRate') * 100).toFixed(0)}%`)
    if (M('backspaceRate') > 0.25) flags.push(`Frequent corrections at ${(M('backspaceRate') * 100).toFixed(0)}%`)
    if (M('intervalCv') > 0.7) flags.push('Irregular typing rhythm')
    if (M('wpm') > 0 && M('wpm') < 15) flags.push(`Slow typing (${M('wpm').toFixed(0)} wpm)`)
  } else {
    label = 'Mood indicators'
    const score = M('depressionScore')
    if (score >= 60) flags.push(`High depression indicator (${score.toFixed(0)}/100)`)
    else if (score >= 35) flags.push(`Moderate depression indicator (${score.toFixed(0)}/100)`)
    if (M('frown') > 0.18) flags.push('Frequent sad expression')
    if (M('brow') > 0.25) flags.push('Furrowed, tense brows')
    if (M('smile') < 0.06 && M('seconds') > 15) flags.push('Little smiling during the session')
    if (M('gazeDown') > 0.45) flags.push('Frequent downward gaze')
    if (M('blinkRate') > 0 && M('blinkRate') < 8) flags.push(`Low blink rate (${M('blinkRate').toFixed(0)}/min)`)
    if (M('flatAffect') > 0.7) flags.push('Flat facial affect')
    if (M('headDown') > 0.5) flags.push('Head tilted down most of the time')
  }

  const level = flags.length >= 3 ? 'urgent' : flags.length >= 2 ? 'watch' : 'nominal'
  const note = flags.length ? `${label} — ${flags.join('; ')}` : `${label}: measurements look normal`
  return { flags, level, note }
}
