using UnityEngine;

public class KnifeThrower : MonoBehaviour
{
    private enum State
    {
        Normal,
        Attack,
    }
    private State state = State.Normal;

    private Body body;
    private Enemy enemy;
    private BoxCollider2D boxCollider;
    private Animator animator;
    public float speedX = 8;
    public GameObject knifePrefab;
    private Player player;
    
    public float playerScanDistance = 12f;
    private float nextThrowTimer = 0;
    private float nextThrowDuration = 0;
    public float nextThrowDurationMin = 1f;
    public float nextThrowDurationMax = 3f;

    private void Awake()
    {
        body = GetComponent<Body>();
        enemy = GetComponent<Enemy>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.Find("Player");
        player = playerObj.GetComponent<Player>();
    }

    private void Update()
    {
        if (player.state == Player.State.Dead)
        {
            animator.speed = 0;
        }
        else if (enemy.isActive)
        {
            switch (state)
            {
                case State.Normal:
                {
                    body.velocity.x = body.DirectionX * speedX;
                    body.Move();

                    if (body.collisionLeft || body.collisionRight)
                    {
                        // Turn around.
                        body.DirectionX = -body.DirectionX;
                    }


                    Vector2 playerScanMin;
                    Vector2 playerScanMax;
                    if (body.DirectionX > 0)
                    {
                        playerScanMin = new Vector2(transform.position.x, boxCollider.bounds.min.y);
                        playerScanMax = new Vector2(transform.position.x + playerScanDistance, boxCollider.bounds.max.y);
                    }
                    else
                    {
                        playerScanMin = new Vector2(transform.position.x - playerScanDistance, boxCollider.bounds.min.y);
                        playerScanMax = new Vector2(transform.position.x, boxCollider.bounds.max.y);
                    }
                    DebugUtility.DrawRect(playerScanMin, playerScanMax, Color.green);
                    bool foundPlayer = Physics2D.OverlapArea(playerScanMin, playerScanMax, 1 << LayerMask.NameToLayer("Player"));

                    nextThrowTimer += Time.deltaTime;

                    if (foundPlayer && nextThrowTimer > nextThrowDuration)
                    {
                        state = State.Attack;
                        animator.Play(Animator.StringToHash("Attack"));
                    }

                    break;
                }

                case State.Attack:
                {
                    if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
                    {
                        state = State.Normal;
                        animator.Play(Animator.StringToHash("Run"));
                    }

                    break;
                }
            }
        }
    }

    private void OnAnimThrowKnife()
    {
        nextThrowTimer = 0;
        nextThrowDuration = Random.Range(1f, 3f);
        GameObject knifeObj = Instantiate(knifePrefab, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity) as GameObject;
        knifeObj.GetComponent<Body>().DirectionX = body.DirectionX;
    }
}
