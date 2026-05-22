using System;
using Godot;

public partial class Movement : CharacterBody3D
{
    [Export]
    private float MaxSpeed = 8f;

    [Export]
    private float GroundAccel = 100f;

    [Export]
    private float AirAccel = 10f;

    [Export]
    private float Friction = 100f;

    [Export]
    private float JumpForce = 30f;

    [Export]
    private float Gravity = 25f;

    [Export]
    private float MouseSensitivity = 0.002f;

    [Export]
    private float JumpBufferTime = 0.15f;

    [Export]
    private Node3D head;

    private float _jumpBufferTimer = 0f;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            RotateY(-motion.Relative.X * MouseSensitivity);
            head.RotateX(-motion.Relative.Y * MouseSensitivity);
            head.Rotation = new Vector3(
                Mathf.Clamp(head.Rotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89)),
                head.Rotation.Y,
                head.Rotation.Z
            );
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;
        Vector2 flatVelocity = new Vector2(velocity.X, velocity.Z);

        if (Input.IsActionJustPressed("jump"))
            _jumpBufferTimer = JumpBufferTime;
        else
            _jumpBufferTimer -= dt;

        if (_jumpBufferTimer > 0 && IsOnFloor())
        {
            velocity.Y = JumpForce;
            _jumpBufferTimer = 0f;
        }

        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 wishDir = GlobalTransform.Basis * new Vector3(input.X, 0, input.Y);
        if (wishDir.LengthSquared() > 0.001f)
            wishDir = wishDir.Normalized();
        else
            wishDir = Vector3.Zero;

        Vector3 hVel = new Vector3(velocity.X, 0, velocity.Z);
        float vVel = velocity.Y;
        bool stop = false;

        if (IsOnFloor())
        {
            if (wishDir.LengthSquared() == 0)
            {
                Vector3 frictionDir = new Vector3(
                    Mathf.Cos(flatVelocity.Angle()),
                    0,
                    Mathf.Sin(flatVelocity.Angle())
                );
                Vector3 frictionForce = frictionDir * Friction;
                if (frictionForce.Length() * dt > hVel.Length())
                    stop = true;
                else
                    hVel -= frictionForce * dt;
            }

            hVel += new Vector3(wishDir.X, 0, wishDir.Z) * GroundAccel * dt;
            if (hVel.Length() > MaxSpeed)
                hVel = hVel.Normalized() * MaxSpeed;
        }
        else
        {
            vVel -= Gravity * dt;

            Vector3 wishDirH = new Vector3(wishDir.X, 0, wishDir.Z);
            float currentSpeedInWishDir = hVel.Dot(wishDirH);
            float addSpeed = MaxSpeed - currentSpeedInWishDir;
            if (addSpeed > 0)
            {
                float accelSpeed = Mathf.Min(AirAccel * dt, addSpeed);
                hVel += wishDirH * accelSpeed;
            }
        }

        if (stop)
        {
            hVel = Vector3.Zero;
        }

        velocity = new Vector3(hVel.X, vVel, hVel.Z);
        Velocity = velocity;
        MoveAndSlide();
    }
}
