import './style.css'

const app = document.querySelector<HTMLDivElement>('#app')!

app.innerHTML = `
  <main class="controller">
    <div class="top">
      <button class="menu">☰</button>
      <span>ZZZ CONTROLLER</span>
      <button class="menu">⚙</button>
    </div>

    <section class="gamepad">

      <div class="shoulders">
        <button data-button="LB">LB</button>
        <button data-button="LT" class="trigger">LT</button>

        <div class="center">
          <button data-button="BACK">BACK</button>
          <button data-button="START">START</button>
        </div>

        <button data-button="RT" class="trigger">RT</button>
        <button data-button="RB">RB</button>
      </div>

      <div class="middle">

        <div class="stick-area">
          <div id="left-stick" class="stick">
            <div class="stick-knob"></div>
          </div>
          <span>LS</span>
        </div>

        <div class="dpad">
          <button data-button="UP">▲</button>
          <button data-button="LEFT">◀</button>
          <button data-button="RIGHT">▶</button>
          <button data-button="DOWN">▼</button>
        </div>

        <div class="stick-area">
          <div id="right-stick" class="stick">
            <div class="stick-knob"></div>
          </div>
          <span>RS</span>
        </div>

        <div class="face-buttons">
          <button data-button="Y">Y</button>
          <button data-button="X">X</button>
          <button data-button="B">B</button>
          <button data-button="A">A</button>
        </div>

      </div>
    </section>

    <div id="status">USB DISCONNECTED</div>
  </main>
`

function sendButton(button: string, state: 'down' | 'up') {
  console.log(button, state)
}

document.querySelectorAll<HTMLButtonElement>('[data-button]').forEach(button => {
  const name = button.dataset.button!

  button.addEventListener('pointerdown', e => {
    e.preventDefault()
    button.classList.add('pressed')
    sendButton(name, 'down')
  })

  button.addEventListener('pointerup', e => {
    e.preventDefault()
    button.classList.remove('pressed')
    sendButton(name, 'up')
  })

  button.addEventListener('pointercancel', () => {
    button.classList.remove('pressed')
    sendButton(name, 'up')
  })
})

function setupStick(id: string) {
  const stick = document.getElementById(id)!
  const knob = stick.querySelector<HTMLElement>('.stick-knob')!

  let active = false

  const move = (x: number, y: number) => {
    const rect = stick.getBoundingClientRect()
    const centerX = rect.width / 2
    const centerY = rect.height / 2

    let dx = x - (rect.left + centerX)
    let dy = y - (rect.top + centerY)

    const max = rect.width * 0.32
    const distance = Math.sqrt(dx * dx + dy * dy)

    if (distance > max) {
      dx = (dx / distance) * max
      dy = (dy / distance) * max
    }

    knob.style.transform = `translate(${dx}px, ${dy}px)`

    const normalizedX = Math.round((dx / max) * 32767)
    const normalizedY = Math.round((-dy / max) * 32767)

    console.log(id, normalizedX, normalizedY)
  }

  stick.addEventListener('pointerdown', e => {
    active = true
    stick.setPointerCapture(e.pointerId)
    move(e.clientX, e.clientY)
  })

  stick.addEventListener('pointermove', e => {
    if (active) move(e.clientX, e.clientY)
  })

  const release = () => {
    active = false
    knob.style.transform = 'translate(0, 0)'
    console.log(id, 0, 0)
  }

  stick.addEventListener('pointerup', release)
  stick.addEventListener('pointercancel', release)
}

setupStick('left-stick')
setupStick('right-stick')