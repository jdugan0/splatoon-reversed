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
    private float WallFriction = 20f;

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

    [Export]
    private float DashSpeed = 16f;

    [Export]
    private float DashCoolDown = 0.05f;

    [Export]
    private int DashMaxAmount = 3;

    [Export]
    private float DashTime = 1;

    [Export]
    private float DashRechargeTime;

    [Export]
    private float WallJumpForce = 15f;

    [Export]
    private float WallLookAngleDeg = 10f;

    [Export]
    private float JumpBoostForce = 5f;

    [Export]
    private Camera3D camera;

    private int currentDashes = 3;
    private float dashCoolDownTimer;
    private float dashRechargeTimer;
    private float dashTimer;
    private Vector3 dashDir;
    private bool _wasOnWall = false;
    private float _wallSlideSpeed = 0f;

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
        // GD.Print(Velocity.Length());
        float dt = (float)delta;
        Vector3 velocity = Velocity;

        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 wishDir = GlobalTransform.Basis * new Vector3(input.X, 0, input.Y);
        if (wishDir.LengthSquared() > 0.001f)
            wishDir = wishDir.Normalized();
        else
            wishDir = Vector3.Zero;

        if (IsOnWallOnly())
        {
            wishDir = Vector3.Zero;
        }

        Vector3 hVel = new Vector3(velocity.X, 0, velocity.Z);
        float vVel = velocity.Y;

        // Dash timers
        bool wasDashing = dashTimer > 0;
        if (dashTimer > 0)
            dashTimer -= dt;
        if (wasDashing && dashTimer <= 0)
            dashCoolDownTimer = DashCoolDown;

        if (dashCoolDownTimer > 0)
            dashCoolDownTimer -= dt;

        if (currentDashes < DashMaxAmount)
        {
            dashRechargeTimer += dt;
            if (dashRechargeTimer >= DashRechargeTime)
            {
                currentDashes++;
                dashRechargeTimer = 0f;
            }
        }

        if (
            Input.IsActionJustPressed("dash")
            && currentDashes > 0
            && dashTimer <= 0
            && dashCoolDownTimer <= 0
            && !IsOnWallOnly()
        )
        {
            Vector3 forward = new Vector3(
                -GlobalTransform.Basis.Z.X,
                0,
                -GlobalTransform.Basis.Z.Z
            ).Normalized();
            dashDir = wishDir.LengthSquared() > 0.001f ? wishDir : forward;
            dashTimer = DashTime;
            currentDashes--;
        }

        if (dashTimer > 0)
        {
            hVel = dashDir * DashSpeed;
            if (!IsOnFloor())
                vVel -= Gravity * dt;
        }
        else
        {
            bool stop = false;
            if (IsOnFloor())
            {
                if (wishDir.LengthSquared() == 0 || hVel.Length() > MaxSpeed)
                {
                    Vector2 flatVelocity = new(hVel.X, hVel.Z);
                    Vector3 frictionDir = new(
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
                if (
                    (hVel + new Vector3(wishDir.X, 0, wishDir.Z) * GroundAccel * dt).Length()
                    <= MaxSpeed
                )
                {
                    hVel += new Vector3(wishDir.X, 0, wishDir.Z) * GroundAccel * dt;
                }
                else
                {
                    float l = hVel.Length();
                    hVel += new Vector3(wishDir.X, 0, wishDir.Z) * GroundAccel * dt;
                    hVel = hVel.Normalized() * l;
                }
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
                hVel = Vector3.Zero;
        }
        if (IsOnWallOnly())
        {
            Vector3 wallNormal = GetWallNormal();
            Vector3 camForward3D = -camera.GlobalTransform.Basis.Z;
            Vector3 camForwardH = new Vector3(camForward3D.X, 0, camForward3D.Z).Normalized();
            float dotWithNormal = camForwardH.Dot(wallNormal);
            float threshold = -Mathf.Cos(Mathf.DegToRad(WallLookAngleDeg));

            if (!_wasOnWall)
                _wallSlideSpeed = new Vector3(velocity.X, 0, velocity.Z).Length() * 1.5f;
            _wallSlideSpeed = Mathf.Max(0f, _wallSlideSpeed - WallFriction * dt);

            if (dotWithNormal < threshold)
            {
                GD.Print("BANG!");
                hVel = Vector3.Zero;
            }
            else
            {
                Vector3 slideDirH = camForwardH - wallNormal * dotWithNormal;
                if (slideDirH.LengthSquared() > 0.001f)
                    hVel = slideDirH.Normalized() * _wallSlideSpeed;
            }

            vVel = 0f;
        }
        if (Input.IsActionJustPressed("jump"))
            _jumpBufferTimer = JumpBufferTime;
        else
            _jumpBufferTimer -= dt;

        if (_jumpBufferTimer > 0 && IsOnFloor())
        {
            hVel += wishDir * JumpBoostForce;
            vVel = JumpForce;
            _jumpBufferTimer = 0f;
        }
        else if (_jumpBufferTimer > 0 && IsOnWallOnly())
        {
            Vector3 wallNormal = GetWallNormal();
            vVel = JumpForce;
            hVel.X = wallNormal.X * WallJumpForce;
            hVel.Z = wallNormal.Z * WallJumpForce;
            _jumpBufferTimer = 0f;
        }

        Velocity = new Vector3(hVel.X, vVel, hVel.Z);
        _wasOnWall = IsOnWallOnly();
        MoveAndSlide();
    }
}
