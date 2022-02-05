using Godot;
using System;

public class Sprite3D : Godot.Sprite3D
{
    private string URL = "ws://192.168.1.9:8887/wsVideo";
    private WebSocketClient ws;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ws = new WebSocketClient();

        ws.Connect("connection_established", this, "_connected");
        ws.Connect("connection_error", this, "_on_connection_error");
        ws.Connect("data_received", this, "_on_received_data");
        
        GD.Print("connection: " + ws.ConnectToUrl(URL) + "!");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        ws.Poll();
    }

    public void _connected(string proto = "")
    {
        GD.Print("connection established !!");
    }

    public void _on_connection_error()
    {
        GD.Print("connection error !!");
    }

    public void _on_received_data()
    {
        var img_bytes = ws.GetPeer(1).GetPacket();
        var image = new Image();
        var error = image.LoadJpgFromBuffer(img_bytes);
        if (error != Error.Ok)
            GD.PushError("Couldn't load the image.");

        var texture = new ImageTexture();
        texture.CreateFromImage(image);

        this.Texture = texture;
    }
}
