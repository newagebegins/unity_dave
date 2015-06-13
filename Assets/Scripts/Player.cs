using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    enum PlayerState
    {
        Normal,
        Shoot,
        OpenDoor,
    }

    private Body body;
    private TreasureDoor treasureDoor;
    private PlayerState state = PlayerState.Normal;
    private Game game;   
    public float accelerationX = 50;
    public float maxVelocityX = 8;
    private BoxCollider2D boxCollider;
    public float jumpVelocityY = 13;
    public float jumpReductionVelocityY = -1;
    private Animator animator;
    public float recoilVelocityX = 5;
    private float directionX = 1;
    public float shotDistance = 30;

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

        maxBullets = ammoHUD.transform.childCount;
        numBullets = maxBullets;
        bulletImages = new Image[maxBullets];
        for (int i = 0; i < bulletImages.Length; ++i)
        {
            Transform bulletTransform = ammoHUD.transform.Find("Bullet" + i);
            Image bulletImage = bulletTransform.GetComponent<Image>();
            bulletImages[i] = bulletImage;
        }
    }

    private void Update()
    {
        float verticalAxis = Input.GetAxisRaw("Vertical"); // Can be -1, 0 or 1
        float horizontalAxis = Input.GetAxisRaw("Horizontal"); // Can be -1, 0 or 1

        body.ApplyHorizontalFriction();

        if (state == PlayerState.Normal)
        {
            if (body.isGrounded)
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

                if ((horizontalAxis > 0 && transform.localScale.x < 0) || (horizontalAxis < 0 && transform.localScale.x > 0))
                {
                    // Flip the sprite.
                    transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

                    // When turning, move the player a little in the direction of movement to avoid stucking in walls.
                    // It is a hack to compensate for the fact that the player's box collider is not centered relative
                    // to the pivot point.
                    transform.Translate(new Vector2(horizontalAxis * 0.3f, 0));
                }

                if (horizontalAxis != 0)
                {
                    directionX = horizontalAxis;
                }
            }

            // Jumping.
            if (body.isGrounded && Input.GetButtonDown("Jump") && verticalAxis >= 0)
            {
                body.velocity.y = jumpVelocityY;
            }

            // Jump down from one-way platforms.
            ignoreOneWayPlatformsTimer -= Time.deltaTime;
            if (!IsIgnoringOneWayPlatforms && Input.GetButton("Jump") && verticalAxis < 0)
            {
                IgnoreOneWayPlatforms();
            }

            // Open treasure door.
            if (body.isGrounded && verticalAxis > 0)
            {
                Collider2D doorCollider = Physics2D.OverlapArea(boxCollider.bounds.min, boxCollider.bounds.max, 1 << LayerMask.NameToLayer("TreasureDoor"));
                if (doorCollider)
                {
                    treasureDoor = doorCollider.GetComponent<TreasureDoor>();
                    if (treasureDoor.IsClosed)
                    {
                        body.velocity.x = 0;
                        state = PlayerState.OpenDoor;
                        openDoorTimer = 0;
                        animator.Play(Animator.StringToHash("OpenDoor"));
                    }
                }
            }
        }

        if (state == PlayerState.OpenDoor)
        {
            openDoorTimer += Time.deltaTime;
            if (openDoorTimer > openDoorDuration)
            {
                state = PlayerState.Normal;
                treasureDoor.Open();
            }
        }

        body.ApplyGravity();

        if (state == PlayerState.Normal)
        {
            // Reduce jump height when "Jump" is not pressed.
            if (body.velocity.y > 0 && !Input.GetButton("Jump"))
            {
                body.velocity.y += jumpReductionVelocityY;
            }
        }

        body.Move(IsIgnoringOneWayPlatforms);

        if (state == PlayerState.Shoot)
        {
            shootTimer += Time.deltaTime;
            if (shootTimer > shootDuration)
            {
                state = PlayerState.Normal;
            }
        }
        
        if (state == PlayerState.Normal)
        {
            if (verticalAxis == 0)
            {
                if (body.isGrounded && body.velocity.x != 0)
                {
                    animator.Play(Animator.StringToHash("Run"));
                }
                else if (!IsReloading)
                {
                    animator.Play(Animator.StringToHash("Idle"));
                }
            }

            if (numBullets > 0 && Input.GetButtonDown("Fire1") && body.isGrounded)
            {
                // Shoot.

                --numBullets;
                shootTimer = 0;
                state = PlayerState.Shoot;
                
                if (verticalAxis == 0)
                {
                    animator.Play(Animator.StringToHash("Shoot"));
                }

                // Recoil
                body.velocity.x = -directionX * recoilVelocityX;

                // Instantiate a gun shot animation.
                GameObject gunShot = Instantiate(gunshotPrefab, shotEnd.position, Quaternion.identity) as GameObject;
                float scaleX = Mathf.Abs(gunShot.transform.localScale.x) * Mathf.Sign(transform.localScale.x);
                gunShot.transform.localScale = new Vector3(scaleX, gunShot.transform.localScale.y, gunShot.transform.localScale.z);

                // Do a raycast to see if we hit something.
                int layerMask = 1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("Enemy");
                Vector2 shotDirection = shotEnd.position - shotStart.position;
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
            if (numBullets < maxBullets && body.isGrounded && body.velocity.x == 0)
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
}
