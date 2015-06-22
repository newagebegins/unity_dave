using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public enum State
    {
        Normal,
        Shoot,
        OpenDoor,
        Dead,
    }
    [HideInInspector]
    public State state = State.Normal;

    private Body body;
    private Door door;
    private Game game;
    public float accelerationX = 50;
    public float maxVelocityX = 8;
    private BoxCollider2D boxCollider;
    public float jumpVelocityY = 13;
    public float jumpReductionVelocityY = -1;
    private Animator animator;
    public float recoilVelocityX = 5;
    public float shotDistance = 30;
    
    private Animator screenFaderAnimator;

    private int maxBullets = 0;
    private int numBullets = 0;
    public GameObject ammoHUD;
    private Image[] bulletImages;
    public float stayToReloadDuration = 0.8f;
    private float stayToReloadTimer = 0;
    private bool IsReloading
    {
        get { return stayToReloadTimer > stayToReloadDuration; }
    }

    public GameObject gunshotPrefab;
    private Transform shotStart;
    private Transform shotEnd;

    private float shootTimer = 0;
    public float shootDuration = 1f;

    private float openDoorTimer = 0;
    public float openDoorDuration = 0.5f;

    public float ignoreOneWayPlatformsDuration = 0.1f;
    private float ignoreOneWayPlatformsTimer = 0;
    private bool IsIgnoringOneWayPlatforms
    {
        get { return ignoreOneWayPlatformsTimer > 0; }
    }
    private void IgnoreOneWayPlatforms()
    {
        ignoreOneWayPlatformsTimer = ignoreOneWayPlatformsDuration;
    }

    private void Awake()
    {
        body = GetComponent<Body>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        shotStart = transform.Find("ShotStart");
        shotEnd = transform.Find("ShotEnd");
        game = Camera.main.GetComponent<Game>();
        
        GameObject screenFader = GameObject.Find("ScreenFader");
        screenFaderAnimator = screenFader.GetComponent<Animator>();

        maxBullets = ammoHUD.transform.childCount;
        numBullets = maxBullets;
        bulletImages = new Image[maxBullets];
        for (int i = 0; i < bulletImages.Length; ++i)
        {
            Transform bulletTransform = ammoHUD.transform.Find("Bullet" + i);
            Image bulletImage = bulletTransform.GetComponent<Image>();
            bulletImages[i] = bulletImage;
        }

        screenFaderAnimator.Play(Animator.StringToHash("FadeToClear"));
    }

    private void Update()
    {
        switch (state)
        {
            case State.Normal:
            {
                body.Move(IsIgnoringOneWayPlatforms);
                
                float verticalAxis = Input.GetAxisRaw("Vertical");
                float horizontalAxis = Input.GetAxisRaw("Horizontal");

                // Snap values for analog sticks.
                if (verticalAxis < 0)
                {
                    verticalAxis = -1;
                }
                else if (verticalAxis > 0)
                {
                    verticalAxis = 1;
                }

                if (horizontalAxis < 0)
                {
                    horizontalAxis = -1;
                }
                else if (horizontalAxis > 0)
                {
                    horizontalAxis = 1;
                }

                if (body.IsGrounded)
                {
                    if (verticalAxis > 0)
                    {
                        animator.Play(Animator.StringToHash("AimUp"));
                    }
                    else if (verticalAxis < 0)
                    {
                        animator.Play(Animator.StringToHash("AimDown"));
                    }
                }
                if (verticalAxis == 0)
                {
                    // Horizontal acceleration.
                    body.velocity.x += horizontalAxis * accelerationX * Time.deltaTime;
                    body.velocity.x = Mathf.Clamp(body.velocity.x, -maxVelocityX, maxVelocityX);
                }
                if (horizontalAxis != 0)
                {
                    body.DirectionX = horizontalAxis;
                }

                // Jumping.
                if (body.IsGrounded && Input.GetButtonDown("Jump") && verticalAxis >= 0)
                {
                    body.velocity.y = jumpVelocityY;
                }

                // Jump down from one-way platforms.
                ignoreOneWayPlatformsTimer -= Time.deltaTime;
                if (!IsIgnoringOneWayPlatforms && Input.GetButton("Jump") && verticalAxis < 0)
                {
                    IgnoreOneWayPlatforms();
                }

                // Reduce jump height when "Jump" is not pressed.
                if (body.velocity.y > 0 && !Input.GetButton("Jump"))
                {
                    body.velocity.y += jumpReductionVelocityY;
                }

                // Open a door.
                if (body.IsGrounded && verticalAxis > 0)
                {
                    Collider2D doorCollider = Physics2D.OverlapArea(boxCollider.bounds.min, boxCollider.bounds.max, 1 << LayerMask.NameToLayer("Door"));
                    if (doorCollider)
                    {
                        door = doorCollider.GetComponent<Door>();
                        if (door.isClosed)
                        {
                            body.velocity.x = 0;
                            state = State.OpenDoor;
                            openDoorTimer = 0;
                            animator.Play(Animator.StringToHash("OpenDoor"));
                        }
                        else
                        {
                            if (door.GetComponent<ExitDoor>())
                            {
                                animator.Play(Animator.StringToHash("OpenDoor"));
                                Kill();
                            }
                        }
                    }
                }

                if (verticalAxis == 0)
                {
                    if (body.IsGrounded && body.velocity.x != 0)
                    {
                        animator.Play(Animator.StringToHash("Run"));
                    }
                    else if (!IsReloading)
                    {
                        animator.Play(Animator.StringToHash("Idle"));
                    }
                }

                if (numBullets > 0 && Input.GetButtonDown("Fire1") && body.IsGrounded)
                {
                    // Shoot.

                    --numBullets;
                    shootTimer = 0;
                    state = State.Shoot;

                    if (verticalAxis == 0)
                    {
                        animator.Play(Animator.StringToHash("Shoot"));
                    }

                    // Recoil
                    body.velocity.x = -body.DirectionX * recoilVelocityX;

                    // Instantiate a gun shot animation.
                    GameObject gunShot = Instantiate(gunshotPrefab, shotEnd.position, Quaternion.identity) as GameObject;
                    float scaleX = Mathf.Abs(gunShot.transform.localScale.x) * Mathf.Sign(transform.localScale.x);
                    gunShot.transform.localScale = new Vector3(scaleX, gunShot.transform.localScale.y, gunShot.transform.localScale.z);

                    // Do a raycast to see if we hit something.
                    int layerMask = 1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("Enemy");
                    Vector2 shotDirection = shotEnd.position - shotStart.position;
                    Debug.DrawRay(shotStart.position, shotDirection * shotDistance, Color.magenta, 0.3f);
                    RaycastHit2D hit = Physics2D.Raycast(shotStart.position, shotDirection, shotDistance, layerMask);
                    if (hit)
                    {
                        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                        {
                            Enemy enemy = hit.collider.GetComponent<Enemy>();
                            enemy.Hit();
                        }
                        game.CreateProjectileExplosion(hit.point);
                    }
                }

                // Ammo reloading.
                if (numBullets < maxBullets && body.IsGrounded && body.velocity.x == 0 && verticalAxis == 0)
                {
                    if (!IsReloading)
                    {
                        stayToReloadTimer += Time.deltaTime;
                        if (IsReloading)
                        {
                            animator.Play(Animator.StringToHash("Reload"));
                        }
                    }
                }
                else
                {
                    stayToReloadTimer = 0;
                }

                // Update ammo HUD.
                for (int i = 0; i < bulletImages.Length; ++i)
                {
                    bulletImages[i].color = i < numBullets ? Color.white : Color.black;
                }
                
                break;
            }

            case State.Shoot:
            {
                body.Move(IsIgnoringOneWayPlatforms);
                shootTimer += Time.deltaTime;
                if (shootTimer > shootDuration)
                {
                    state = State.Normal;
                }
                break;
            }

            case State.OpenDoor:
            {
                openDoorTimer += Time.deltaTime;
                if (openDoorTimer > openDoorDuration)
                {
                    state = State.Normal;
                    door.Open();
                }
                break;
            }

            case State.Dead:
            {
                animator.speed = 0;

                AnimatorStateInfo info = screenFaderAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName("FadeToBlack") && info.normalizedTime > 1)
                {
                    Application.LoadLevel(Application.loadedLevel);
                }
                
                break;
            }

            default:
            {
                Debug.LogError("Unhandled player state " + state.ToString());
                break;
            }
        }
    }

    public void OnOneBulletReloaded()
    {
        if (numBullets < maxBullets)
        {
            numBullets++;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Collectible"))
        {
            Collectible collectible = collision.GetComponent<Collectible>();
            game.CreatePoints(collision.transform.position, collectible.scoreValue);
            Destroy(collision.gameObject);
        }
    }

    public void Kill()
    {
        state = State.Dead;
        screenFaderAnimator.Play(Animator.StringToHash("FadeToBlack"));
    }
}
