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
    private float WallFriction = 20f;

    [Export]
    private float JumpForce = 30f;

    [Export]
    private float Gravity = 25f;

    [Export]
    private float MouseSensitivity = 1.5f;

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
    private float FootstepTime;

    [Export]
    private float FootStepAmt = 3.5f;

    [Export]
    private float WallJumpForce = 15f;

    [Export]
    private float WallLookAngleDeg = 10f;

    [Export]
    private float JumpBoostForce = 5f;

    [Export]
    private Camera3D camera;

    [Export]
    private Camera3D viewModelCamera;

    private int currentDashes = 3;
    private float dashCoolDownTimer;
    private float dashRechargeTimer;
    private float footstepTimer;
    private float dashTimer;
    private Vector3 dashDir;
    private bool _wasOnWall = false;
    private float _wallSlideSpeed = 0f;
    private Vector3 _wallSlideDir = Vector3.Zero;
    private bool _wasOnFloor = false;
    private bool _wasMovingFast = false;

    [Export]
    private float MovingFastThreshold = 10f;
    private bool dirty = false;

    [Export]
    private Area3D area;

    private bool dashing = false;

    private float cachedVvel = 0;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            RotateY(-motion.Relative.X / viewportSize.X * MouseSensitivity);
            head.RotateX(-motion.Relative.Y / viewportSize.X * MouseSensitivity);
            head.Rotation = new Vector3(
                Mathf.Clamp(head.Rotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89)),
                head.Rotation.Y,
                head.Rotation.Z
            );
        }
    }

    public void CheckDirty()
    {
        dirty = false;
        foreach (var body in area.GetOverlappingBodies())
        {
            if (body.GetParent() is DirtyWall wall)
            {
                wall.Paint(GlobalPosition, DirtyWall.SplatType.WALK);
                if (wall.SampleDirtAt(GlobalPosition) is float d && d > 0.5f)
                {
                    // GD.Print(dirty);
                    // GD.Print("DEBUG");
                    dirty = true;
                }
            }
        }
    }

    public bool GetDirty()
    {
        return dirty;
    }

    public void PaintJumpSplat()
    {
        foreach (var body in area.GetOverlappingBodies())
        {
            if (body.GetParent() is DirtyWall wall)
            {
                wall.Paint(GlobalPosition, DirtyWall.SplatType.JUMP);
            }
        }
    }

    public override void _Process(double delta)
    {
        viewModelCamera.GlobalTransform = camera.GlobalTransform;
    }

    private Vector3 nextFrameForce = Vector3.Zero;

    public void AddForce(Vector3 force)
    {
        nextFrameForce += force;
    }

    public override void _PhysicsProcess(double delta)
    {
        CheckDirty();
        //GD.Print(dirty);
        GD.Print(CleaningManager.I.CleanFraction());
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
        // FOOTSTEP SFX
        if (velocity.Length() > 0 && IsOnFloor() && !wasDashing)
        {
            footstepTimer += dt;
            if (footstepTimer >= FootStepAmt / velocity.Length())
            {
                if (dirty)
                    AudioManager.instance.PlaySFX("step-concrete-heavy");
                else
                    AudioManager.instance.PlaySFX("step-concrete-clean");
                footstepTimer = 0f;
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
            Vector3 forward = (-camera.GlobalTransform.Basis.Z).Normalized();
            dashDir = forward;
            dashTimer = DashTime;
            cachedVvel = vVel;
            currentDashes--;
            AudioManager.instance.PlaySFX("generic-woosh");
        }
        else if (
            Input.IsActionJustPressed("dash")
            && dashTimer > 0
            && !IsOnWallOnly()
            && currentDashes > 0
        )
        {
            dashTimer = DashTime;
            currentDashes--;
            dashDir = wishDir;
            AudioManager.instance.PlaySFX("generic-woosh");
        }

        if (dashTimer > 0)
        {
            Vector3 dashVel = dashDir * DashSpeed;
            hVel = new Vector3(dashVel.X, 0, dashVel.Z);
            vVel = dashVel.Y;
            dashing = true;
        }
        else
        {
            bool stop = false;
            if (dashing)
            {
                dashing = false;
                vVel = Math.Min(cachedVvel, vVel);
            }
            if (IsOnFloor())
            {
                if (!_wasOnFloor)
                    AudioManager.instance.PlaySFX("move-land");
                _wasOnFloor = true;
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
                _wasOnFloor = false;
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
            {
                _wallSlideSpeed = new Vector3(velocity.X, 0, velocity.Z).Length();
                Vector3 slideDirH = camForwardH - wallNormal * dotWithNormal;
                _wallSlideDir =
                    slideDirH.LengthSquared() > 0.001f ? slideDirH.Normalized() : Vector3.Zero;
                AudioManager.instance.PlaySFX("move-wallslide-loop");
            }

            _wallSlideSpeed = Mathf.Max(0f, _wallSlideSpeed - WallFriction * dt);

            if (dotWithNormal < threshold)
            {
                hVel = Vector3.Zero;
            }
            else
            {
                hVel = _wallSlideDir * _wallSlideSpeed;
            }

            vVel = 0f;
        }
        else
        {
            AudioManager.instance.CancelSFX("move-wallslide-loop");
        }
        if (Input.IsActionJustPressed("jump"))
            _jumpBufferTimer = JumpBufferTime;
        else
            _jumpBufferTimer -= dt;

        if (_jumpBufferTimer > 0 && IsOnFloor())
        {
            PaintJumpSplat();
            hVel += wishDir * JumpBoostForce;
            vVel = JumpForce;
            _jumpBufferTimer = 0f;
            AudioManager.instance.PlaySFX("generic-woosh");
            dashTimer = 0;
        }
        else if (_jumpBufferTimer > 0 && IsOnWallOnly())
        {
            Vector3 wallNormal = GetWallNormal();
            PaintJumpSplat();
            vVel = JumpForce;
            hVel.X = wallNormal.X * WallJumpForce;
            hVel.Z = wallNormal.Z * WallJumpForce;
            _jumpBufferTimer = 0f;
            AudioManager.instance.PlaySFX("generic-woosh");
            dashTimer = 0;
        }

        if (Velocity.Length() > 10)
        {
            if (!_wasMovingFast)
                AudioManager.instance.PlaySFX("move-wind-loop");
            _wasMovingFast = true;
        }
        else
        {
            _wasMovingFast = false;
            AudioManager.instance.CancelSFX("move-wind-loop");
        }
        hVel.X += nextFrameForce.X * dt;
        if (nextFrameForce.Y * dt > 0.1)
        {
            vVel += nextFrameForce.Y * dt;
        }

        hVel.Z += nextFrameForce.Z * dt;
        nextFrameForce = Vector3.Zero;
        Velocity = new Vector3(hVel.X, vVel, hVel.Z);
        _wasOnWall = IsOnWallOnly();
        MoveAndSlide();
    }
}
