using System.Reflection;
using Mimic.Animation;
using Mimic.InputSystem;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsNoClipMovement
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo CamRootField =
            AccessTools.Field(typeof(ProtoActor), "camRoot")!;

        private static readonly FieldInfo CharacterControllerField =
            AccessTools.Field(typeof(ProtoActor), "_cc")!;

        private static readonly FieldInfo FallingField =
            AccessTools.Field(typeof(ProtoActor), "falling")!;

        private static readonly FieldInfo PuppetField =
            AccessTools.Field(typeof(ProtoActor), "puppet")!;

        private static readonly FieldInfo HubInputManagerField =
            typeof(Hub).GetField("inputman", InstanceFlags)!;

        private static readonly FieldInfo HubConsoleField =
            typeof(Hub).GetField("console", InstanceFlags)!;

        private static readonly Action<ProtoActor> RotateByInput =
            CreateInstanceAction(AccessTools.Method(typeof(ProtoActor), "RotateByInput")!);

        private static readonly Func<ProtoActor, Vector2> GetMovementInput =
            CreateInstanceFunc<Vector2>(AccessTools.Method(typeof(ProtoActor), "GetMovementInput")!);

        private static readonly Func<ProtoActor, bool, bool> ProcessSprintKey =
            (Func<ProtoActor, bool, bool>)Delegate.CreateDelegate(
                typeof(Func<ProtoActor, bool, bool>),
                null,
                AccessTools.Method(typeof(ProtoActor), "ProcessSprintKey", [typeof(bool)])!);

        private static readonly Func<ProtoActor, float> CalculateSpeed =
            CreateInstanceFunc<float>(AccessTools.Method(typeof(ProtoActor), "CaculateSpeed")!);

        private static readonly Action<ProtoActor> EnableCc =
            CreateInstanceAction(AccessTools.Method(typeof(ProtoActor), "EnableCCWithSafeSpawnIfAvatar")!);

        private static readonly Func<ProtoActor, float> GetWalkSpeed =
            CreateInstanceGetter<float>(AccessTools.Property(typeof(ProtoActor), "walkSpeed")!);

        private static readonly Func<ProtoActor, float> GetRunSpeed =
            CreateInstanceGetter<float>(AccessTools.Property(typeof(ProtoActor), "runSpeed")!);

        private static Type? _consoleIsActiveMethodOwner;
        private static MethodInfo? _consoleIsActiveMethod;
        private static int _consoleCheckFrame = -1;
        private static bool _consoleBlocking;

        internal static bool ShouldReplaceControl(ProtoActor actor)
        {
            if (WebDashboardHostCheatsRuntime.IsRoomTransitionSuspended
                || !WebDashboardHostCheatsRuntime.IsNoClipActiveForActor(actor)
                || !actor.AmIAvatar()
                || actor.controlMode != ProtoActor.ControlMode.Manual
                || actor.dead
                || actor.dontMoveFlag)
            {
                return false;
            }

            if (IsConsoleBlocking())
            {
                return false;
            }

            return true;
        }

        private static bool IsConsoleBlocking()
        {
            int frame = Time.frameCount;
            if (frame == _consoleCheckFrame)
            {
                return _consoleBlocking;
            }

            _consoleCheckFrame = frame;
            _consoleBlocking = false;

            if (Hub.s == null || HubConsoleField == null)
            {
                return false;
            }

            object? console = HubConsoleField.GetValue(Hub.s);
            if (console == null)
            {
                return false;
            }

            Type consoleType = console.GetType();
            if (_consoleIsActiveMethodOwner != consoleType)
            {
                _consoleIsActiveMethodOwner = consoleType;
                _consoleIsActiveMethod = AccessTools.Method(consoleType, "IsConsoleActive");
            }

            if (_consoleIsActiveMethod?.Invoke(console, null) is true)
            {
                _consoleBlocking = true;
            }

            return _consoleBlocking;
        }

        internal static void PrepareActor(long playerUid)
        {
            if (playerUid == 0)
            {
                return;
            }

            try
            {
                GameMainBase? main = GameSessionAccess.TryGetPdata()?.main;
                if (main?.GetActorByPlayerUID(playerUid) is ProtoActor actor)
                {
                    SetCharacterControllerEnabled(actor, enabled: false);
                }
            }
            catch
            {
                /* avatar may not exist yet */
            }
        }

        internal static void Apply(ProtoActor actor)
        {
            RotateByInput(actor);

            Vector2 movementInput = GetMovementInput(actor);
            _ = ProcessSprintKey(actor, actor.isSprinting);
            float speed = CalculateSpeed(actor) * PlayerTuningResolver.NoClipSpeedMultiplier;

            Transform? camRoot = CamRootField.GetValue(actor) as Transform;
            if (camRoot == null)
            {
                return;
            }

            Vector3 moveDirection = camRoot.forward * movementInput.y + camRoot.right * movementInput.x;
            InputManager? inputManager = GetInputManager();
            if (inputManager != null && inputManager.isPressed(InputAction.Jump))
            {
                moveDirection += Vector3.up;
            }

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                actor.transform.position += moveDirection.normalized * speed * Time.deltaTime;
            }

            FallingField.SetValue(actor, 0f);

            try
            {
                if (PuppetField.GetValue(actor) is PuppetScript puppet)
                {
                    Vector3 velocity = moveDirection.sqrMagnitude > 0.0001f
                        ? moveDirection.normalized * speed
                        : Vector3.zero;
                    puppet.Move(velocity, GetWalkSpeed(actor), GetRunSpeed(actor));
                }
            }
            catch
            {
                /* puppet may be mid-reset while CC is disabled */
            }
        }

        internal static void RestoreCharacterController(ProtoActor actor)
        {
            EnableCc(actor);
        }

        internal static void TryRestoreActor(long playerUid)
        {
            if (playerUid == 0)
            {
                return;
            }

            try
            {
                GameMainBase? main = GameSessionAccess.TryGetPdata()?.main;
                if (main?.GetActorByPlayerUID(playerUid) is ProtoActor actor)
                {
                    RestoreCharacterController(actor);
                }
            }
            catch
            {
                /* avatar may not exist yet */
            }
        }

        private static InputManager? GetInputManager()
        {
            if (Hub.s == null || HubInputManagerField == null)
            {
                return null;
            }

            return HubInputManagerField.GetValue(Hub.s) as InputManager;
        }

        private static void SetCharacterControllerEnabled(ProtoActor actor, bool enabled)
        {
            object? controller = CharacterControllerField.GetValue(actor);
            if (controller == null)
            {
                return;
            }

            PropertyInfo? enabledProperty = controller.GetType().GetProperty("enabled", InstanceFlags);
            if (enabledProperty?.GetValue(controller) is bool isEnabled && isEnabled != enabled)
            {
                enabledProperty.SetValue(controller, enabled);
            }
        }

        private static Action<ProtoActor> CreateInstanceAction(MethodInfo method)
        {
            return (Action<ProtoActor>)Delegate.CreateDelegate(typeof(Action<ProtoActor>), null, method);
        }

        private static Func<ProtoActor, T> CreateInstanceFunc<T>(MethodInfo method)
        {
            return (Func<ProtoActor, T>)Delegate.CreateDelegate(typeof(Func<ProtoActor, T>), null, method);
        }

        private static Func<ProtoActor, T> CreateInstanceGetter<T>(PropertyInfo property)
        {
            MethodInfo getter = property.GetGetMethod(nonPublic: true)
                ?? throw new InvalidOperationException($"Getter missing for {property.DeclaringType?.Name}.{property.Name}");
            return CreateInstanceFunc<T>(getter);
        }
    }
}
