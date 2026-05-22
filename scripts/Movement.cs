using Godot;

public partial class Movement : CharacterBody3D
{
    [Export]
    private float MoveSpeed = 8f;

    [Export]
    private float JumpForce = 8f;

    [Export]
    private float Gravity = 25f;

    [Export]
    private float MouseSensitivity = 0.002f;

    [Export]
    private Node3D head;

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

        if (!IsOnFloor())
            velocity.Y -= Gravity * dt;

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpForce;

        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 moveDir = (GlobalTransform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
        velocity.X = moveDir.X * MoveSpeed;
        velocity.Z = moveDir.Z * MoveSpeed;

        Velocity = velocity;
        MoveAndSlide();
    }
}
