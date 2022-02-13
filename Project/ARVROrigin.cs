using Godot;
using System;

public class ARVROrigin : Godot.ARVROrigin
{
    float defaultAngle = 0.5F;
    float defaultSpeedForward = 1.0F;
    float defaultSpeedBackward = -0.7F;

    float currentAngle = 0.0F;
    float currentThrottle = 0.0F;


    WSMessage wsMsg = new WSMessage();
    bool isMessageQueued = true;


    //private string URL = "ws://donkey-new392:8887/wsDrive";
    private string URL = "ws://192.168.1.9:8887/wsDrive";
    private WebSocketClient ws;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.Singleton.Connect("joy_connection_changed", this, nameof(_on_joy_connection_changed));

        var leftController = this.GetNode<ARVRController>(new NodePath("ARVRController_Left"));
        leftController.Connect("button_pressed", this, nameof(_on_button_pressed),
            new Godot.Collections.Array("left pad"));

        var rightController = this.GetNode<ARVRController>(new NodePath("ARVRController_Right"));
        rightController.Connect("button_pressed", this, nameof(_on_button_pressed),
            new Godot.Collections.Array(new object[] { "right pad" }));


        ws = new WebSocketClient();
        GD.Print("connection: " + ws.ConnectToUrl(URL) + "!");

        ws.Connect("connection_established", this, "_connected");
    }


    bool isJoypadConnectionNotified = false;
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (Input.GetConnectedJoypads().Count > 0)
        {
            if (!isJoypadConnectionNotified)
            {
                GD.Print($"2 joypads connected");
                isJoypadConnectionNotified = true;
            }

            if (isMessageQueued)
            {
                ws.Poll();
                GD.Print($"message sent: { wsMsg.stringify() }");

                isMessageQueued = false;
            }
        }
    }


    public void _connected(string proto = "")
    {
        GD.Print("connection established - drive !!");
    }


    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventJoypadMotion)
        {
            var leftPadIndex = 0;
            var rightPadIndex = 1;

            var threshold = 0.60;


            bool toTheLeft = false;
            bool toTheRight = false;
            bool toTheUp = false;
            bool toTheDown = false;


            // ------- left pad
            var leftRightAxis = Input.GetJoyAxis(leftPadIndex, (int)JoystickList.Axis0);
            if (leftRightAxis < -threshold)
            {
                toTheLeft = true;
                GD.Print($"device 0 (left pad): 'LEFT' {leftRightAxis}");
            }
            else if (leftRightAxis > threshold)
            {
                toTheRight = true;
                GD.Print($"device 0 (left pad): 'RIGHT' {leftRightAxis}");
            }


            var upDownAxis = Input.GetJoyAxis(leftPadIndex, (int)JoystickList.Axis1);
            if (upDownAxis > threshold)
            {
                toTheUp = true;
                GD.Print($"device 0 (left pad): 'UP' {upDownAxis}");
            }
            else if (upDownAxis < -threshold)
            {
                toTheDown = true;
                GD.Print($"device 0 (left pad): 'DOWN' {upDownAxis}");
            }
            







            // ------- right pad
            leftRightAxis = Input.GetJoyAxis(rightPadIndex, (int)JoystickList.Axis0);
            if (leftRightAxis < -threshold)
            {
                toTheLeft = true;
                GD.Print($"device 1 (right pad): 'LEFT' {leftRightAxis}");
            }
            else if (leftRightAxis > threshold)
            {
                toTheRight = true;
                GD.Print($"device 1 (right pad): 'RIGHT' {leftRightAxis}");
            }


            upDownAxis = Input.GetJoyAxis(rightPadIndex, (int)JoystickList.Axis1);
            if (upDownAxis > threshold)
            {
                toTheUp = true;
                GD.Print($"device 1 (right pad): 'UP' {upDownAxis}");
            }
            else if (upDownAxis < -threshold)
            {
                toTheDown = true;
                GD.Print($"device 1 (right pad): 'DOWN' {upDownAxis}");
            }




            if (toTheLeft)
            {
                currentAngle = -defaultAngle;
                SendMessage();
            }
            else if (toTheRight)
            {
                currentAngle = defaultAngle;
                SendMessage();
            }
            else
            {
                currentAngle = 0;
                SendMessage();
            }


            if (toTheUp)
            {
                currentThrottle = defaultSpeedForward;
                SendMessage();
            }
            else if (toTheDown)
            {
                currentThrottle = defaultSpeedBackward;
                SendMessage();
            }
            else
            {
                currentThrottle = 0;
                SendMessage();
            }
        }
    }


    public void _on_joy_connection_changed(int device, bool connected)
    {
        if (connected)
        {
            GD.Print($"gamepad {device}: connected");
            if (Input.IsJoyKnown(0))
                GD.Print($"gamepad {device} recognized {Input.GetJoyName(0)}");
            else
                GD.Print($"gamepad {device} not recognized");
        }
        else
            GD.Print($"gamepad {device} : disconnected");
    }


    public void _on_button_pressed(int button, string deviceName)
    {
        GD.Print($"_on_button_pressed ({deviceName}), id: {button} -  name: {Input.GetJoyButtonString(button)}");
    }


    public void SendMessage()
    {
        wsMsg.angle = currentAngle;
        wsMsg.throttle = currentThrottle;
        var msg = wsMsg.stringify();
        var err = ws.GetPeer(1).PutPacket(msg.ToUTF8());
        GD.Print("put package - " + err);

        isMessageQueued = true;
    }


    public class WSMessage
    {
        public float angle { get; set; }
        public float throttle { get; set; }
        public bool recording => false;
        public string drive_mode => "user";


        public string stringify()
        {
            var str =
                "{\"angle\": " + this.angle.ToString() + ", " +
                "\"throttle\": " + this.throttle.ToString() + ", " +
                "\"recording\": false, " +
                "\"drive_mode\": \"" + this.drive_mode + "\"}";
            return str;
        }
    }
}
