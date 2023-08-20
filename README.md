# Joystick Axis Pulsator
A standalone Windows application that reads the axis states of a joystick, and sends pulsating keystrokes to the game, thus imitating the analog values.

<img width="417" height="400" src="https://www.onekilobit.eu/media/uploads/joystickPulsator/main.png">

### Planned features
- Wide-range joystick and gamepad detection 
- Basic control support (pitch/yaw/roll)
- Dynamic axis value support
- Throttle support (missing)
- Device calibration
- Input and output frequency tuning
- Window selection (missing)
- Optional toggle button

<img align="left" width="524" height="144" src="https://www.onekilobit.eu/media/uploads/joystickPulsator/freq.png">

<br>
<br>
<br>
<br>
<br>
<br>


### Method
<img align="right" width="446" height="300" src="https://www.onekilobit.eu/media/uploads/joystickPulsator/pwm.png">
This solution uses PWM (Pulse-Width Modulation) to convert the analog (eg. 50%, 75%, 33%) signals of the joystick to alternating keypresses.

### Disclaimer
Since the program will blindly spam your system with keys at a high rate, it may will cause some issues with your applications and/or system.
<br>Please note that I do not take resposibility for any problem caused by the behavior described above (but will gladly help with them, if possible).

<br>
<br>
<img width="781" height="510" src="https://www.onekilobit.eu/media/uploads/joystickPulsator/cal.jpg">
