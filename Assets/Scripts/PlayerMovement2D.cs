using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float speed = 4f;
    [SerializeField] private bool constrainToRoom = true;
    [SerializeField] private Vector2 roomMin = new Vector2(-17.2f, -9.4f);
    [SerializeField] private Vector2 roomMax = new Vector2(17.2f, 9.4f);

    [Header("Walk animation")]
    [Tooltip("Контроллер можно заменить и редактировать через обычное окно Animator.")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [Tooltip("Исходные кадры AnimSekta смотрят влево, поэтому это поле выключено.")]
    [SerializeField] private bool faceRightByDefault;
    [Tooltip("Не позволяет цвету старого SpriteRenderer окрашивать кадры анимации.")]
    [SerializeField] private bool forceOriginalSpriteColors = true;

    [Header("Footstep sounds")]
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.45f;
    [SerializeField, Min(0.05f)] private float footstepInterval = 0.32f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource footstepSource;
    private AudioClip[] footstepClips;
    private float nextFootstepTime;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && forceOriginalSpriteColors)
        {
            spriteRenderer.color = Color.white;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 10;
        }
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
        if (animatorController == null)
        {
            animatorController = Resources.Load<RuntimeAnimatorController>("PlayerWalk/PlayerWalkController");
        }
        if (animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = animatorController;
        }
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;
        footstepClips = Resources.LoadAll<AudioClip>("GameAudio/Footsteps");
    }

    private void Update()
    {
        movement = Vector2.zero;

        if (GameDayState.IsDialogueActive)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            movement.y = 1;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            movement.y = -1;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            movement.x = -1;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            movement.x = 1;
        }

        movement = movement.normalized;
        UpdateWalkAnimation();
    }

    private void LateUpdate()
    {
        if (spriteRenderer != null && forceOriginalSpriteColors && spriteRenderer.color != Color.white)
        {
            spriteRenderer.color = Color.white;
        }
    }

    private void UpdateWalkAnimation()
    {
        if (spriteRenderer == null) return;

        if (movement.x != 0f)
        {
            bool movingRight = movement.x > 0f;
            spriteRenderer.flipX = faceRightByDefault ? !movingRight : movingRight;
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("Speed", movement.sqrMagnitude);
        }
        UpdateFootsteps();
    }

    private void UpdateFootsteps()
    {
        if (movement.sqrMagnitude < 0.001f || footstepClips == null || footstepClips.Length == 0) return;
        if (Time.time < nextFootstepTime) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        footstepSource.pitch = Random.Range(0.94f, 1.06f);
        footstepSource.PlayOneShot(clip, footstepVolume);
        nextFootstepTime = Time.time + footstepInterval;
    }

    private void FixedUpdate()
    {
        if (GameDayState.IsDialogueActive)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 nextPosition = rb.position + movement * speed * Time.fixedDeltaTime;

        if (constrainToRoom)
        {
            nextPosition.x = Mathf.Clamp(nextPosition.x, roomMin.x, roomMax.x);
            nextPosition.y = Mathf.Clamp(nextPosition.y, roomMin.y, roomMax.y);
        }

        rb.MovePosition(nextPosition);
    }
}
