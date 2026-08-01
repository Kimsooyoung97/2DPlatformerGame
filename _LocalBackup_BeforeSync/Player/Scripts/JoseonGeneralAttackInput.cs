using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Animator))]
public sealed class JoseonGeneralAttackInput : MonoBehaviour
{
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        bool leftMousePressed = false;

#if ENABLE_INPUT_SYSTEM
        leftMousePressed = Mouse.current != null &&
                           Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        leftMousePressed = Input.GetMouseButtonDown(0);
#endif

        if (leftMousePressed)
        {
            animator.Play(AttackState, 0, 0f);
            animator.Update(0f);
        }
    }
}
