import { cpSync, existsSync, mkdirSync } from 'node:fs'
const src = 'node_modules/@mediapipe/tasks-vision/wasm'
const dst = 'public/mediapipe/wasm'
if (existsSync(src)) {
  mkdirSync(dst, { recursive: true })
  for (const f of ['vision_wasm_internal.js', 'vision_wasm_internal.wasm']) {
    if (existsSync(`${src}/${f}`)) cpSync(`${src}/${f}`, `${dst}/${f}`)
  }
  console.log('MediaPipe wasm copied to public/mediapipe/wasm')
}
