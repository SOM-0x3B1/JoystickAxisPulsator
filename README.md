# Joystick Axis Pulsator
A standalone Windows application that reads the axis states of a joystick, and sends pulsating keystrokes to the game, thus imitating the analog values.

<img align="right" width="446" height="240" src="https://www.onekilobit.eu/media/uploads/joystickPulsator/mainMenu.jpg">

### Planned features
- Wide-range joystick and gamepad detection
- Basic control support (pitch/yaw/roll)
- Dynamic axis value support
- Throttle support
- Device calibration
- Input and output frequency tuning
- Window selection
- Optional toggle button

<img align="right" width="446" height="300" src="https://www.onekilobit.eu/media/uploads/joystickPulsator/pwm.png">

### Method
This solution uses PWM (Pulse-Width Modulation) to convert the analog (eg. 50%, 75%, 33%) signals of the joystick to alternating keypresses.

### Disclaimer
Since the program will blindly spam the selected window with keys at a very high rate, it may will cause some issues with your applications and/or system.
Please note that I do not take resposibility for any problem caused by the behavior described above (but will gladly help with them, if possible).
