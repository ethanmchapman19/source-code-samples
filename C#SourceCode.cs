/// <summary>
/// Source code from multiple projects (academic, gam jams, personal, etc.)
/// Code will be seperated by comments describing what the project was and what the code was for
/// </summary>

/// Code form an academic project March to April 2026 making a physics based
/// Gunship Prototype using jerk, acceleration, and drag to create interesting movement.
/// There was also a focus on modular entity design for things like weapons and abilities
/// which made creating the multi-entity boss extremely easy

public class GameManager : MonoBehaviour
{
    public static GameManager GMInstance;

    public CameraBehavior GameCamera = null;
    public GameObject CurrentShip = null;
    private Spaceship ShipLogic = null;
    private Rigidbody2D ShipPhysics = null;

    private List<GameObject> Ships = new List<GameObject>();
    public int CurrentShipIndex = -1;

    //Performance Data
    public TextMeshProUGUI PerformaceText;
    public TextMeshProUGUI DisplayText;

    public int FramesWindow = 200;
    private List<float> PreviousFrameRates = new List<float>();
    public float WorstTimeCheck = 10.0f;
    private float MeanRate = 0.0f;
    private float MedianRate = 0.0f;
    private float WorstRate = 0.0f;

    private float GameTime = 0.0f;
    private float Timer = 0.0f;

    ////Telemetry
    private StreamWriter DataStream;
    //Ship Values
    public float ShipChaingunTime = 0.0f;
    public int ShipMissleFires = 0;
    public float ShipDamageTaken = 0.0f;
    public int ShipDeaths = 0;
    public float SmallShipTime = 0.0f;
    public float MediumShipTime = 0.0f;
    public float LargeShipTime = 0.0f;
    //Boss Values
    public float BossChaingunTime = 0.0f;
    public int BossMissleFires = 0;
    public int BossGrenadesLaunched = 0;
    public float BossDamageTaken = 0.0f;

    [SerializeField]
    private float XBoundary = 30.0f;
    [SerializeField]
    private float YBoundary = 30.0f;

    public float TextTimer = 0.0f;

    public bool AutomateMode = false;

    private void Awake()
    {
        if (GMInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        GMInstance = this;

        DontDestroyOnLoad(gameObject);

        Directory.CreateDirectory(Application.dataPath + "\\Telemetry");

        string filename = DateTime.Now.ToString("MM-dd_tthh.mm.ss");

        DataStream = new StreamWriter($"{Application.dataPath}\\Telemetry\\Telemetry_{filename}.csv");

        DataStream.WriteLine("TelemetryData");
        DataStream.Write("Time, Frame Rate, Mean Frame Rate, Median Frame Rate, Worst Frame Rate");
        DataStream.Write(",Ship Position, Ship Direction, Chaingun Time, Missle Fires, Damage Taken, NumberDeaths, Small Ship Time, Medium Ship Time, Big Ship Time");
        DataStream.Write(",Boss Position, Boss Direction, Boss State, Boss Chaingun Time, Boss Missle Fires, Boss Grenades Launched, Boss Damage Taken\n");
    }

    void Start()
    {
        Ships.Add(Resources.Load<GameObject>("Prefabs/SmallShip"));
        Ships.Add(Resources.Load<GameObject>("Prefabs/MediumShip"));
        Ships.Add(Resources.Load<GameObject>("Prefabs/LargeShip"));

        SwapShip(0);

        TextTimer = 5.5f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwapShip(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwapShip(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwapShip(2);
        }
        ClampShipPosition();


        float dt = Time.unscaledDeltaTime;
        switch (CurrentShipIndex)
        {
            case 0:
                SmallShipTime += dt;
                break;
            case 1:
                MediumShipTime += dt;
                break;
            case 2:
                LargeShipTime += dt;
                break;
        }

        GameTime += dt;
        Timer += dt;

        if (Input.GetKeyDown(KeyCode.P))
        {
            PerformaceText.gameObject.SetActive(!PerformaceText.gameObject.activeSelf);
        }
        float frameRate = 1.0f / Time.deltaTime;

        PreviousFrameRates.Add(frameRate);

        MeanRate = 0.0f;

        List<float> SortedRates = new List<float>();
        int startIndex = Mathf.Max(0, PreviousFrameRates.Count - FramesWindow);
        for (int i = startIndex; i < PreviousFrameRates.Count; i++)
        {
            MeanRate += PreviousFrameRates[i];
            SortedRates.Add(PreviousFrameRates[i]);
        }
        MeanRate /= Mathf.Min(PreviousFrameRates.Count, FramesWindow);

        SortedRates.Sort();
        int medianIndex = (int)(Mathf.Min(SortedRates.Count, FramesWindow) * 0.5f);

        MedianRate = SortedRates[medianIndex];

        if (PerformaceText.gameObject.activeSelf)
        {
            PerformaceText.SetText($"Frame Rate: {frameRate:F2}\nMean Rate: {MeanRate:F2}\nMedian Rate: {MedianRate:F2}\nWorst Rate: {WorstRate:F2}");
        }

        if (Timer >= WorstTimeCheck)
        {
            Timer = 0.0f;
            WorstRate = Mathf.Min(PreviousFrameRates.ToArray());

            WriteTelemetry();

            PreviousFrameRates.Clear();
        }

        if (DisplayText.canvasRenderer.GetAlpha() == 1.0f)
        {
            TextTimer -= dt;
            if (TextTimer <= 0.0f)
            {
                TextTimer = 0.0f;
                DisplayText.CrossFadeAlpha(0.0f, 0.5f, true);
            }
        }
    }

    public void SwapShip(int index)
    {
        if (CurrentShip)
            Destroy(CurrentShip);

        CurrentShip = Instantiate(Ships[index]);
        ShipPhysics = CurrentShip.GetComponent<Rigidbody2D>();
        ShipLogic = CurrentShip.GetComponent<Spaceship>();
        CurrentShipIndex = index;

        switch (index)
        {
            case 0:
                DisplayText.SetText("Small Ship");
                break;
            case 1:
                DisplayText.SetText("Medium Ship");
                break;
            case 2:
                DisplayText.SetText("Large Ship");
                break;
        }
        DisplayText.CrossFadeAlpha(1.0f, 0.5f, true);
        TextTimer = 2.5f;
    }

    public IEnumerator ResetPlayer(float waitTime)
    {
        Destroy(CurrentShip, waitTime + 0.15f);
        yield return new WaitForSeconds(waitTime);
        GMInstance.SwapShip(GMInstance.CurrentShipIndex);
    }

    private void ClampShipPosition()
    {
        if (!CurrentShip)
            return;

        Vector2 position = CurrentShip.transform.position;
        Vector2 velocity = ShipPhysics.velocity;
        float MoveDirStrength = 5.0f * Time.deltaTime;

        if (position.x < -XBoundary)
        {
            position.x = -XBoundary;
            velocity.x = Mathf.Max(velocity.x, 0);
            ShipLogic.IMoveDirection.x += MoveDirStrength;
        }
        else if (position.x > XBoundary)
        {
            position.x = XBoundary;
            velocity.x = Mathf.Min(velocity.x, 0);
            ShipLogic.IMoveDirection.x -= MoveDirStrength;
        }

        if (position.y < -YBoundary)
        {
            position.y = -YBoundary;
            velocity.y = Mathf.Max(velocity.y, 0);
            ShipLogic.IMoveDirection.y += MoveDirStrength;
        }
        else if (position.y > YBoundary)
        {
            position.y = YBoundary;
            velocity.y = Mathf.Min(velocity.y, 0);
            ShipLogic.IMoveDirection.y -= MoveDirStrength;
        }

        CurrentShip.transform.position = position;
        ShipPhysics.velocity = velocity;
    }

    public Vector2 GetWorldBoundaries()
    {
        return new Vector2(XBoundary, YBoundary);
    }

    private void WriteTelemetry()
    {
        //Write General Game Data
        DataStream.Write($"{GameTime:F3}, {(PreviousFrameRates[PreviousFrameRates.Count - 1]):F3}, {MeanRate:F2}, {MedianRate:F2}, {WorstRate:F2}");

        //Write Ship Values
        if (CurrentShip)
        {
            Vector3 pos = CurrentShip.transform.position;
            DataStream.Write($", {pos.x:F3}; {pos.y:F3}; {pos.z:F3}, {Vector3.Angle(Vector3.up, CurrentShip.transform.up):F3}");
        }
        else
        {
            DataStream.Write(", 0; 0; 0, 0");
        }
        DataStream.Write($", {ShipChaingunTime:F3},{ShipMissleFires},{ShipDamageTaken:F3},{ShipDeaths},{SmallShipTime:F3},{MediumShipTime:F3},{LargeShipTime:F3}");

        //Write Boss Values
        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss != null)
        {
            Vector3 bossPos = boss.transform.position;
            DataStream.Write($", {bossPos.x:F3}; {bossPos.y:F3}; {bossPos.z:F3}, {Vector3.Angle(Vector3.up, boss.transform.up):F3}");
            Boss bossComp = boss.GetComponent<Boss>();
            if (bossComp.IsAggroed)
            {
                DataStream.Write(", Attacking");
            }
            else
            {
                DataStream.Write(", Patrolling");
            }
        }
        else
        {
            DataStream.Write(", 0; 0; 0, 0, Dead");
        }
        DataStream.Write($", {BossChaingunTime:F3},{BossMissleFires}, {BossGrenadesLaunched}, {BossDamageTaken:F3}");

        DataStream.Write("\n");
    }
    public void OnDestroy()
    {
        WriteTelemetry();
        DataStream.Close();
    }
}

public class Spaceship : MonoBehaviour
{
    private Rigidbody2D physics = null;
    private Health health = null;

    [SerializeField]
    private float Jerk = 200.0f;
    [SerializeField]
    private float MaxAcceleration = 1000.0f;
    [SerializeField]
    private float Drag = 1.0f;

    [SerializeField]
    private float RotateJerk = 10000.0f;
    [SerializeField]
    private float MaxRotationalAcceleration = 25000.0f;
    [SerializeField]
    private float RotationalDrag = 1.0f;

    [SerializeField]
    private float CollisionDamage = 10.0f;

    private float Acceleration = 0.0f;
    private float AngularAcceleration = 0.0f;

    private Chaingun[] guns = null;
    private MissleLauncher[] launchers = null;

    private GameObject Explosion = null;

    private float RoamTimer = 0.0f;
    private bool IRetreat = false;
    public Vector3 IMoveDirection = Vector3.zero;
    private GameObject ITarget = null;

    void Start()
    {
        physics = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        health.DeathEffect = OnDeath;

        guns = GetComponentsInChildren<Chaingun>();
        foreach (Chaingun gun in guns)
        {
            gun.parent = gameObject;
        }
        launchers = GetComponentsInChildren<MissleLauncher>();
        foreach (MissleLauncher launcher in launchers)
        {
            launcher.parent = gameObject;
        }

        Explosion = Resources.Load<GameObject>("Prefabs/ExplosionPlayer");
    }

    void FixedUpdate()
    {
        if (health.Stunned)
            return;

        //// Deal with movement
        float moveDirection = 0.0f;
        if (!GMInstance.AutomateMode)
        {
            if (Input.GetKey(KeyCode.W))
            {
                moveDirection = 1.0f;

            }
            else if (Input.GetKey(KeyCode.S))
            {
                moveDirection = -0.5f;
            }
        }
        else
        {
            float dotProduct = Vector3.Dot(transform.up, IMoveDirection.normalized);
            if (dotProduct > 0.3f)
            {
                moveDirection = 1.0f;
            }
            else if (dotProduct < -0.3f)
            {
                moveDirection = -1.0f;
            }
        }

        if ((moveDirection > 0.0f && Acceleration < 0.0f)
            || (moveDirection < 0.0f && Acceleration > 0.0f)
            || moveDirection == 0.0f)
        {
            Acceleration = 0.0f;
        }

        if (moveDirection != 0.0f)
        {
            Acceleration += Jerk * moveDirection * Time.deltaTime;
            Acceleration = Mathf.Clamp(Acceleration, -MaxAcceleration, MaxAcceleration);
        }

        Vector2 forward = new Vector2(transform.up.x, transform.up.y);
        forward *= Acceleration * Time.deltaTime;
        physics.velocity += forward;

        //Drag
        physics.velocity += Drag * Time.deltaTime * -physics.velocity;

        //// Deal with rotation
        float turnDirection = 0.0f;
        if (!GMInstance.AutomateMode)
        {
            if (Input.GetKey(KeyCode.D))
            {
                turnDirection = -1.0f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                turnDirection = 1.0f;
            }
        }
        else
        {
            float angle = Vector3.SignedAngle(transform.up, IMoveDirection, transform.forward);
            if (angle < 0.0f)
            {
                turnDirection = -1.0f;
            }
            else if (angle > 0.0f)
            {
                turnDirection = 1.0f;
            }
        }

        if ((turnDirection > 0.0f && physics.angularVelocity < 0.0f)
            || (turnDirection < 0.0f && physics.angularVelocity > 0.0f)
            || turnDirection == 0.0f)
        {
            AngularAcceleration = 0.0f;
        }
        if (turnDirection != 0.0f)
        {
            AngularAcceleration += RotateJerk * Time.deltaTime * turnDirection;
            AngularAcceleration = Mathf.Clamp(AngularAcceleration, -MaxRotationalAcceleration, MaxRotationalAcceleration);
            physics.angularVelocity += AngularAcceleration * Time.deltaTime;
        }

        physics.angularVelocity -= RotationalDrag * physics.angularVelocity * Time.deltaTime;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            GMInstance.AutomateMode = !GMInstance.AutomateMode;
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            health.Invincible = !health.Invincible;
        }

        if (health.Stunned)
            return;

        UpdateIMoveDirection();

        //Weaponry
        if (!GMInstance.AutomateMode)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                GMInstance.ShipChaingunTime += Time.unscaledDeltaTime;
                foreach (Chaingun gun in guns)
                {
                    gun.Firing = true;
                }
            }
            else
            {
                foreach (Chaingun gun in guns)
                {
                    gun.Firing = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                bool fired = false;
                foreach (MissleLauncher launcher in launchers)
                {
                    if (launcher.Fire())
                        fired = true;
                }
                if (fired)
                    ++GMInstance.ShipMissleFires;
            }
        }
        else
        {
            if (ITarget)
            {
                Vector3 toTarget = (ITarget.transform.position - transform.position);

                if (toTarget.magnitude <= 20.0f)
                {
                    if (Vector3.Dot(toTarget, transform.up) > 0.0f)
                    {
                        GMInstance.ShipChaingunTime += Time.unscaledDeltaTime;
                        foreach (Chaingun gun in guns)
                        {
                            gun.Firing = true;
                        }
                    }
                    bool fired = false;
                    foreach (MissleLauncher launcher in launchers)
                    {
                        if (launcher.Fire())
                            fired = true;
                    }
                    if (fired)
                        ++GMInstance.ShipMissleFires;
                }
                else
                {
                    foreach (Chaingun gun in guns)
                    {
                        gun.Firing = false;
                    }
                }
            }
            else
            {
                foreach (Chaingun gun in guns)
                {
                    gun.Firing = false;
                }
            }
        }
    }

    private void UpdateIMoveDirection()
    {
        if (GMInstance.AutomateMode)
        {
            if (!ITarget)
            {
                GameObject[] bossParts = GameObject.FindGameObjectsWithTag("Enemy");
                if (bossParts.Length != 0)
                {
                    foreach (GameObject part in bossParts)
                    {

                    }
                    ITarget = bossParts[UnityEngine.Random.Range(0, bossParts.Length)];
                }
                else
                {
                    ITarget = GameObject.FindGameObjectWithTag("Boss");
                }
            }

            if (ITarget)
            {
                Vector3 toTarget = ITarget.transform.position - transform.position;
                float distance = toTarget.magnitude;
                if (distance < 10.0f && !IRetreat)
                {
                    IRetreat = true;
                }
                if (IRetreat)
                {
                    if (distance > 50.0f)
                    {
                        IRetreat = false;
                        IMoveDirection = toTarget;
                    }
                    else
                        IMoveDirection = -1.0f * toTarget;
                }
                else
                {
                    IMoveDirection = toTarget;
                }
            }
            else
            {
                RoamTimer -= Time.deltaTime;
                if (RoamTimer <= 0.0f)
                {
                    RoamTimer = UnityEngine.Random.Range(3.0f, 10.0f);
                    float direction = UnityEngine.Random.Range(-180.0f, 180.0f);
                    IMoveDirection = Quaternion.AngleAxis(direction, Vector3.forward) * transform.up;
                    IMoveDirection.z = 0.0f;
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.GetComponent<Bullet>() && !collision.gameObject.GetComponent<Missle>())
        {
            Health health = GetComponent<Health>();
            if (health)
            {
                Vector3 direction = (transform.position - collision.transform.position).normalized;
                health.TakeDamage(CollisionDamage, (70.0f / physics.mass) * direction);
            }
        }
    }

    private void SpawnExplosion()
    {
        Instantiate(Explosion, transform.position + new Vector3(0, 0, -0.5f), Quaternion.identity);
        StartCoroutine(GMInstance.GameCamera.CameraShake(0.5f));
    }
    private void OnDeath(float time)
    {
        SpawnExplosion();
        Invoke("SpawnExplosion", time / 2.0f);
        Invoke("SpawnExplosion", time);
        physics.angularDrag = 0.1f;
        physics.angularVelocity = UnityEngine.Random.Range(200.0f, 1000.0f);
        Drag = 0.0f;
        ++GMInstance.ShipDeaths;
        GMInstance.DisplayText.SetText("You DIED!");
        GMInstance.DisplayText.CrossFadeAlpha(1.0f, 0.5f, true);
        GMInstance.TextTimer = time;
        StartCoroutine(ResetPlayer(time));
    }

    private IEnumerator ResetPlayer(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        StartCoroutine(GMInstance.ResetPlayer(1.5f));
    }
}

public class CameraBehavior : MonoBehaviour
{
    private Camera CameraComp;

    public AnimationCurve ShakeCurve;

    //Follow Variables
    [SerializeField]
    private float Interpolant = 1.0f;
    [SerializeField]
    private float MaxAccel = 1.0f;

    //Zoom Variables
    [SerializeField]
    private float Padding = 5.0f;
    [SerializeField]
    private float ZoomInInterpolant = 1.5f;
    [SerializeField]
    private float ZoomOutInterpolant = 0.7f;
    [SerializeField]
    private float MaxZoom = 25.0f;

    private float Zoom = 10.0f;

    private float SpeedLimit = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        CameraComp = gameObject.GetComponent<Camera>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (GMInstance.CurrentShip == null)
            return;

        FollowShip();
        CalculateZoom();
        AdjustZoom();

        float half_height = CameraComp.orthographicSize;
        float half_width = half_height * CameraComp.aspect;

        transform.position = new Vector3(
        Mathf.Clamp(transform.position.x, -GMInstance.GetWorldBoundaries().x + half_width, GMInstance.GetWorldBoundaries().x - half_width),
        Mathf.Clamp(transform.position.y, -GMInstance.GetWorldBoundaries().y + half_height, GMInstance.GetWorldBoundaries().y - half_height),
        transform.position.z
        );
    }

    void FollowShip()
    {
        //Find the offset to the target
        Vector3 targetPos = GMInstance.CurrentShip.transform.position;
        Vector2 adjust = targetPos - transform.position;
        float distance2D = adjust.magnitude; //Use later to detect overshooting

        //Determine amount to interpolate
        adjust *= Interpolant * Time.deltaTime;

        //Adjust if it is going too fast
        if (adjust.magnitude > SpeedLimit)
            adjust = adjust.normalized * SpeedLimit;

        //Adjust if it is going too slow so it doesn't take forever at the end
        if (adjust.magnitude < 0.5f * Time.deltaTime)
            adjust = adjust.normalized * 0.5f * Time.deltaTime;

        //Save old position for speed limit calculation below
        var oldPosition = transform.position;

        //Move towards the target, but not along the Z axis
        if (adjust.magnitude < distance2D)
            transform.Translate(adjust.x, adjust.y, 0.0f);
        else //Don't overshoot the target
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        //Limit how fast the camera can accelerate so it doesn't feel too jumpy
        SpeedLimit = (transform.position - oldPosition).magnitude * Time.deltaTime + MaxAccel * Time.deltaTime;
    }

    void CalculateZoom()
    {
        Vector3 targetPos = GMInstance.CurrentShip.transform.position;

        float leftEdge = float.MaxValue;
        float rightEdge = -float.MaxValue;
        float bottomEdge = float.MaxValue;
        float topEdge = -float.MaxValue;

        float ShipPadding = Padding + (Padding * GMInstance.CurrentShipIndex);

        leftEdge = Mathf.Min(leftEdge, targetPos.x - ShipPadding);
        rightEdge = Mathf.Max(rightEdge, targetPos.x + ShipPadding);
        bottomEdge = Mathf.Min(bottomEdge, targetPos.y - ShipPadding);
        topEdge = Mathf.Max(topEdge, targetPos.y + ShipPadding);

        float leftOfCamera = rightEdge - transform.position.x;
        float rightOfCamera = transform.position.x - leftEdge;
        float aboveCamera = topEdge - transform.position.y;
        float belowCamera = transform.position.y - bottomEdge;
        Zoom = Mathf.Max(leftOfCamera, rightOfCamera, aboveCamera, belowCamera);

        Zoom = Mathf.Min(Zoom, MaxZoom);
    }

    void AdjustZoom()
    {
        //Find the target zoom level
        float zoomAdjust = Zoom - CameraComp.orthographicSize;

        //Use later to detect overshooting
        float zoomDistance = Mathf.Abs(zoomAdjust);

        //Determine amount to interpolate
        if (zoomAdjust > 0.0f)
            zoomAdjust *= ZoomOutInterpolant * Time.deltaTime;
        else
            zoomAdjust *= ZoomInInterpolant * Time.deltaTime;

        //Adjust if it is going too slow
        if (zoomAdjust < 0.5f * Time.deltaTime && zoomAdjust > 0)
            zoomAdjust = 0.5f * Time.deltaTime;
        else if (zoomAdjust > -0.01f && zoomAdjust < 0)
            zoomAdjust = -0.5f * Time.deltaTime;

        //Move towards the target zoom level
        if (Mathf.Abs(zoomAdjust) < zoomDistance)
            CameraComp.orthographicSize += zoomAdjust;
        else //Don't overshoot the target zoom level
            CameraComp.orthographicSize = Zoom;
    }

    public IEnumerator CameraShake(float Duration)
    {
        Vector3 start = transform.position;
        float timer = 0.0f;
        while (timer < Duration)
        {
            timer += Time.deltaTime;
            if ((start - transform.position).magnitude > 1.0f)
            {
                start = transform.position;
            }
            float strength = ShakeCurve.Evaluate(timer / Duration);
            transform.position = start + (Random.insideUnitSphere * strength);
            yield return null;
        }
    }
}

public class Chaingun : MonoBehaviour
{

    public bool Firing = false;
    public GameObject parent = null;

    [SerializeField]
    private float FireRate = 5.0f;
    [SerializeField]
    private float RampUp = 25.0f;
    [SerializeField]
    private float RampDown = 6.25f;
    [SerializeField]
    private float FireDrift = 5.0f;

    private float CurrentFireRate = 0.0f;
    private float Cooldown = 1.0f;

    [SerializeField]
    private GameObject BulletPrefab = null;

    void Start()
    {
        if (BulletPrefab == null)
            BulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
    }

    public void Fire(Quaternion drift)
    {
        GameObject bulletObj = GameObject.Instantiate(BulletPrefab, transform.position + new Vector3(0, 0, 0.1f), transform.rotation * drift);
        if (bulletObj != null)
        {
            Bullet b = bulletObj.GetComponent<Bullet>();
            if (b)
            {
                b.parent = parent;
            }
        }
    }

    public void FixedUpdate()
    {
        float dt = Time.deltaTime;

        if (Firing)
        {
            if (CurrentFireRate < FireRate)
            {
                CurrentFireRate += dt * FireRate / RampUp;
            }
            if (CurrentFireRate > FireRate)
            {
                CurrentFireRate = FireRate;
            }
        }
        else if (CurrentFireRate > 0.0f)
        {
            CurrentFireRate -= dt * FireRate / RampDown;
        }
        else
        {
            CurrentFireRate = 0.0f;
        }

        Cooldown -= dt * CurrentFireRate;

        if (Cooldown <= 0.0f)
        {
            float drift = FireDrift * CurrentFireRate / FireRate;
            Fire(Quaternion.AngleAxis(Random.Range(-drift, drift), Vector3.forward));
            Cooldown = 1.0f;
        }
    }
}

public class Bullet : MonoBehaviour
{
    public GameObject parent;
    private Rigidbody2D physics = null;
    private Rigidbody2D parentPhysics = null;

    [SerializeField]
    private float BulletSpeed = 100.0f;
    [SerializeField]
    private float BulletDamage = 1.0f;
    [SerializeField]
    private float KnockbackStrength = 25.0f;
    [SerializeField]
    private float StunTime = 0.5f;
    [SerializeField]
    private float Lifetime = 2.0f;

    private float Timer = 0.0f;
    void Start()
    {
        physics = GetComponent<Rigidbody2D>();
        float parentVelMag = 0.0f;
        parentPhysics = parent.GetComponent<Rigidbody2D>();
        if (parentPhysics != null)
        {
            parentVelMag = parentPhysics.velocity.magnitude;
        }
        physics.velocity = (BulletSpeed + parentVelMag) * new Vector2(transform.up.x, transform.up.y);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject != parent)
        {
            Health h = collision.gameObject.GetComponent<Health>();
            if (h)
            {
                h.TakeDamage(BulletDamage, KnockbackStrength * transform.up, StunTime);
                Destroy(gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        Timer += Time.deltaTime;

        if (Timer >= Lifetime)
        {
            Destroy(gameObject);
        }
    }
}

class MissleLauncher : MonoBehaviour
{
    public GameObject parent = null;

    [SerializeField]
    private int MissleCount = 5;
    [SerializeField]
    private float SpawnDelay = 0.5f;

    [SerializeField]
    private GameObject Missle = null;

    private List<GameObject> Missles = new List<GameObject>();
    private bool Firing = false;
    private float Timer = 0;

    private void Start()
    {
        if (Missle == null)
            Missle = Resources.Load<GameObject>("Prefabs/MisslePlayer");
    }

    private void FixedUpdate()
    {

        if (Missles.Count != 0)
        {
            for (int i = 0; i < Missles.Count; i++)
            {
                if (Missles[i] == null)
                {
                    Missles.RemoveAt(i);
                    --i;
                }
            }
        }
        if (Firing)
        {
            if (Missles.Count >= MissleCount)
            {
                Firing = false;
                return;
            }

            Timer -= Time.deltaTime;

            if (Timer <= 0.0f)
            {
                Missles.Add(Instantiate(Missle, transform.position, transform.rotation));
                Missles[Missles.Count - 1].GetComponent<Missle>().parent = parent;
                Timer = SpawnDelay;
            }
        }
    }

    public bool Fire()
    {
        if (Missles.Count == 0)
        {
            Firing = true;
            Timer = 0.0f;
            return true;
        }
        return false;
    }
}

class Missle : MonoBehaviour
{
    public GameObject parent = null;
    private Rigidbody2D physics = null;
    private GameObject target = null;

    private GameObject effect = null;

    [SerializeField]
    private string[] TargetTags = null;
    [SerializeField]
    private int Damage = 5;
    [SerializeField]
    private float StunTime = 1.8f;
    [SerializeField]
    private float KnockbackStrength = 150.0f;
    [SerializeField]
    private float Lifetime = 5.0f;
    [SerializeField]
    private float ArmTime = 1.0f;
    [SerializeField]
    private float Jerk = 50;
    [SerializeField]
    private float MaxAcceleration = 100;
    [SerializeField]
    private float Drag = 50;
    [SerializeField]
    private float TurnSpeed = 5;
    [SerializeField]
    private bool IgnoreBullets = false;

    private float Timer = 0;
    private float Acceleration = 0;


    private void Start()
    {
        physics = GetComponent<Rigidbody2D>();
        effect = Resources.Load<GameObject>("Prefabs/ExplosionEffect");

        FindTarget();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject != parent)
        {
            Health h = collision.gameObject.GetComponent<Health>();
            if (h)
            {
                h.TakeDamage(Damage, KnockbackStrength * transform.up, StunTime);
                if (effect)
                    Instantiate(effect, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
            if (!IgnoreBullets)
            {
                Bullet b = collision.gameObject.GetComponent<Bullet>();
                if (b)
                {
                    if (effect)
                        Instantiate(effect, transform.position, Quaternion.identity);
                    Destroy(gameObject);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        float dt = Time.deltaTime;

        //Move
        Acceleration += Jerk * dt;
        Acceleration = Mathf.Min(Acceleration, MaxAcceleration);
        Vector2 forward = transform.up;
        physics.velocity += Acceleration * dt * forward;
        physics.velocity += Drag * dt * -physics.velocity;

        Timer += Time.deltaTime;

        if (Timer >= ArmTime)
        {
            if (target)
            {
                Vector3 targetPos = target.transform.position;
                Vector3 currPos = gameObject.transform.position;
                targetPos.z = 0;
                currPos.z = 0;
                Vector3 toTarget = targetPos - currPos;
                Vector3 currDir = transform.up;

                transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.RotateTowards(currDir, toTarget, TurnSpeed * dt, 1.0f));
            }
            else
            {
                FindTarget();
            }
        }


        if (Timer >= Lifetime + ArmTime)
        {
            Destroy(gameObject);
        }
    }

    private void FindTarget()
    {
        foreach (string tag in TargetTags)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
            float distance = float.MaxValue;
            foreach (GameObject enemy in targets)
            {
                float newDistance = (enemy.transform.position - gameObject.transform.position).magnitude;
                if (newDistance < distance)
                {
                    distance = newDistance;
                    target = enemy;
                }
            }
            if (target)
                return;
        }
    }
}

public class Health : MonoBehaviour
{
    [SerializeField]
    private bool DamageTrail = true;
    [SerializeField]
    private float Amount = 10;
    [SerializeField]
    private float MaxHealth = 20;
    [SerializeField]
    private float RegenerationSpeed = 0.0f;
    [SerializeField]
    private float RegenWaitTime = 1.5f;
    [SerializeField]
    private float BTHealthPercent = 0.25f;
    [SerializeField]
    private float BTScalePercent = 0.4f;
    [SerializeField]
    private float BTDuration = 3.0f;
    [SerializeField]
    private float DeathEffectTime = 2.0f;

    public delegate void DeathEffectFunc(float Time);
    public DeathEffectFunc DeathEffect = null;

    public bool Invincible = false;
    public bool Stunned = false;

    private float TimeToLerp = 3.0f;
    private float Timer = 0.0f;
    private float BTTimer = 0.0f;

    private GameObject TrailPrefab = null;

    private void Start()
    {
        TrailPrefab = Resources.Load<GameObject>("Prefabs/DamageTrail");
    }

    public void TakeDamage(float amount, Vector3 knockback, float StunTime = 0.5f)
    {
        if (Amount <= 0 || Stunned || Invincible)
            return;

        if (gameObject == GMInstance.CurrentShip)
        {
            GMInstance.ShipDamageTaken += amount;
        }
        if (gameObject.CompareTag("Boss") || gameObject.CompareTag("Enemy"))
        {
            GMInstance.BossDamageTaken += amount;
        }

        Amount -= amount;
        if (Amount <= 0)
        {
            Stunned = true;
            if (DeathEffect != null)
            {
                DeathEffect(DeathEffectTime);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }
        if (BTTimer <= 0.0f && (Amount / MaxHealth) < BTHealthPercent)
        {
            StartCoroutine(LerpTimeScale(Time.timeScale, BTScalePercent, TimeToLerp + (Time.timeScale - 1.0f)));
            BTTimer = BTDuration;
        }
        if (DamageTrail)
        {
            if (TrailPrefab)
            {
                GameObject trail = Instantiate(TrailPrefab, gameObject.transform);
                Vector3 pos = trail.transform.localPosition;
                pos.z = 0.5f;
                //pos.y -= (transform.localScale.y / 2.8f);
                trail.GetComponent<TrailRenderer>().startWidth = transform.localScale.x;
                trail.transform.localPosition = pos;
                Destroy(trail, 0.5f);
            }
        }
        Knockback(knockback);
        StartCoroutine(HitEffect(0.5f));
        Amount = Mathf.Min(Amount, MaxHealth);
        Timer = RegenWaitTime;
    }

    public void Update()
    {
        if (Stunned)
            return;

        Timer -= Time.unscaledDeltaTime;
        if (RegenerationSpeed > 0.0f && Timer <= 0.0f && Amount < MaxHealth)
        {
            Amount += RegenerationSpeed * Time.deltaTime;
            Amount = Mathf.Min(Amount, MaxHealth);
            if (BTTimer > 0.0f && Time.timeScale != 1.0f && (Amount / MaxHealth) > BTHealthPercent)
            {
                BTTimer = 0.0f;
                StartCoroutine(LerpTimeScale(Time.timeScale, 1.0f, TimeToLerp - (Time.timeScale - 0.5f)));
            }
        }
        if (BTTimer > 0.0f)
        {
            BTTimer -= Time.unscaledDeltaTime;
            if (BTTimer <= 0.0f && Time.timeScale != 1.0f)
            {
                StartCoroutine(LerpTimeScale(Time.timeScale, 1.0f, TimeToLerp - (Time.timeScale - 0.5f)));
            }
        }
    }

    private IEnumerator LerpTimeScale(float Start, float End, float Duration)
    {
        if (Amount > 0.0f)
        {
            float timer = 0f;
            while (timer < Duration)
            {
                timer += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(Start, End, timer / Duration);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                yield return null;
            }

            Time.timeScale = End;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
    }

    private IEnumerator HitEffect(float Duration)
    {
        float timer = 0.0f;
        while (timer < Duration)
        {
            timer += Time.unscaledDeltaTime;
            foreach (SpriteRenderer sprite in gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                Color color = sprite.color;
                color.a = Mathf.Cos(12.0f * Mathf.PI * timer);
                sprite.color = color;
            }
            yield return null;
        }
        foreach (SpriteRenderer sprite in gameObject.GetComponentsInChildren<SpriteRenderer>())
        {
            Color color = sprite.color;
            color.a = 1.0f;
            sprite.color = color;
        }
    }

    public float GetAmount()
    {
        return Amount;
    }

    public void Knockback(Vector3 knockback, float StunTime = 0.3f)
    {
        Stunned = true;
        Rigidbody2D physics = gameObject.GetComponent<Rigidbody2D>();
        if (physics != null)
        {
            physics.AddForce(knockback);
        }
        StartCoroutine(Unstun(StunTime));
    }

    private IEnumerator Unstun(float Duration)
    {
        yield return new WaitForSeconds(Duration);
        Stunned = false;
    }
}

public class BossPart : MonoBehaviour
{
    public GameObject parent = null;

    [SerializeField]
    private float LimitOfRotation = 40.0f;
    [SerializeField]
    private float RotationSpeed = 10.0f;

    private int Direction = 0;

    private Chaingun[] Guns = null;
    private MissleLauncher[] Launchers = null;

    private GameObject Explosion = null;

    public bool FiringGuns = false;
    public bool Active = false;
    private float ToRotate = 0;
    private float PauseTimer = 0.0f;
    private float RotateTimer = 0.0f;

    void Start()
    {
        //Movement
        ToRotate = LimitOfRotation;
        Direction = Random.Range(0, 2);
        if (Direction == 0)
            Direction = -1;

        Guns = GetComponentsInChildren<Chaingun>();
        foreach (Chaingun gun in Guns)
        {
            gun.parent = gameObject;
        }
        Launchers = GetComponentsInChildren<MissleLauncher>();
        foreach (MissleLauncher launcher in Launchers)
        {
            launcher.parent = gameObject;
        }

        Health h = GetComponent<Health>();
        if (h)
        {
            Explosion = Resources.Load<GameObject>("Prefabs/ExplosionBossParts");
            h.DeathEffect = OnDeath;
        }
    }

    void Update()
    {
        if (!Active)
            return;

        float dt = Time.deltaTime;

        //Movement
        if (RotationSpeed != 0.0f && PauseTimer <= 0.0f)
        {
            float RotateAmount = RotationSpeed * dt;
            transform.Rotate(transform.forward, RotateAmount * Direction);
            ToRotate -= RotateAmount;
            if (ToRotate <= 0.0f)
            {
                ToRotate = LimitOfRotation * 2.0f;
                Direction = -Direction;
            }
            if (RotateTimer <= 0.0f && Random.Range(0, 100) < 30)
            {
                PauseTimer = Random.Range(2.0f, 5.0f);
            }
            else if (RotateTimer > 0.0f)
            {
                RotateTimer -= dt;
            }
        }
        else if (PauseTimer > 0.0f)
        {
            PauseTimer -= dt;
            if (PauseTimer <= 0.0f)
            {
                RotateTimer = Random.Range(2.0f, 5.0f);
            }
        }

        //Fire at player if in front
        Vector3 playerDirection = GMInstance.CurrentShip.transform.position - transform.position;
        if (Vector3.Dot(playerDirection, transform.up) > 0.0f)
        {
            if (Guns.Length != 0)
            {
                FiringGuns = true;
                foreach (Chaingun gun in Guns)
                {
                    gun.Firing = true;
                }
            }
            foreach (MissleLauncher launcher in Launchers)
            {
                if (launcher.Fire())
                {
                    ++GMInstance.BossMissleFires;
                }
            }
        }
        else
        {
            if (Guns.Length != 0)
            {
                FiringGuns = false;
            }
            foreach (Chaingun gun in Guns)
            {
                gun.Firing = false;
            }
        }
    }
    private void OnDeath(float time)
    {
        Instantiate(Explosion, transform.position, Quaternion.identity);
        Destroy(gameObject, 0.1f);
    }
}

public class Boss : MonoBehaviour
{
    private List<BossPart> Parts = new List<BossPart>();
    private BossGrenadeLauncher BGL = null;
    private List<GameObject> ExplosionPoints = new List<GameObject>();
    private Health BossHealth = null;
    private Rigidbody2D BossPhysics = null;

    private GameObject Explosion = null;
    private GameObject ExplosionFinal = null;

    [SerializeField]
    private float Jerk = 100.0f;
    [SerializeField]
    private float MaxAcceleration = 1000.0f;
    [SerializeField]
    private float Drag = 5.0f;

    [SerializeField]
    private float TurnSpeed = 10.0f;

    [SerializeField]
    private float AggroRange = 20.0f;
    [SerializeField]
    private float StrafeRangeScale = 0.6f;
    [SerializeField]
    private float StrafeMoveScale = 0.2f;
    [SerializeField]
    private float DeaggroRangeScale = 3.0f;

    private float Acceleration = 0.0f;

    //AI variables
    private bool Aggroed = false;
    public bool IsAggroed
    {
        get { return Aggroed; }
    }
    private float PatrolTimer = 0.0f;
    private Vector3 MoveDirection = Vector3.zero;

    private void Start()
    {
        Parts.AddRange(GetComponentsInChildren<BossPart>());
        BGL = GetComponentInChildren<BossGrenadeLauncher>();
        BossHealth = GetComponent<Health>();
        BossPhysics = GetComponent<Rigidbody2D>();
        if (BossHealth)
        {
            Explosion = Resources.Load<GameObject>("Prefabs/ExplosionBoss");
            ExplosionFinal = Resources.Load<GameObject>("Prefabs/ExplosionBossFinal");
            BossHealth.DeathEffect = OnDeath;
        }

        ExplosionPoints.AddRange(GameObject.FindGameObjectsWithTag("BossExplodePoint"));
        for (int i = 0; i < ExplosionPoints.Count; i++)
        {
            int index = Random.Range(i, ExplosionPoints.Count);
            GameObject temp = ExplosionPoints[index];
            ExplosionPoints[index] = ExplosionPoints[i];
            ExplosionPoints[i] = temp;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (Parts.Count == 0)
        {
            if (BossHealth)
            {
                if (BossHealth.Invincible)
                {
                    BossHealth.Invincible = false;
                }
                if (Input.GetKeyDown(KeyCode.K))
                {
                    BossHealth.TakeDamage(1000.0f, Vector3.zero);
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                for (int i = 0; i < Parts.Count; ++i)
                {
                    if (Parts[i])
                    {
                        Parts[i].GetComponent<Health>().TakeDamage(1000.0f, Vector3.zero);
                    }
                    else
                    {
                        Parts.RemoveAt(i);
                        --i;
                    }
                }
            }
        }

        if (!Aggroed)
        {
            Patrol(dt);
            if (GMInstance.CurrentShip && (GMInstance.CurrentShip.transform.position - transform.position).magnitude <= AggroRange)
            {
                Aggroed = true;
                for (int i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i] != null)
                    {
                        Parts[i].Active = true;
                    }
                    else
                    {
                        Parts.RemoveAt(i);
                        --i;
                    }
                }
                BGL.Active = true;
            }
        }
        else
        {
            bool firing = false;
            for (int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i] != null)
                {
                    if (Parts[i].FiringGuns)
                    {
                        firing = true;
                    }
                }
                else
                {
                    Parts.RemoveAt(i);
                    --i;
                }
            }
            if (firing)
            {
                GMInstance.BossChaingunTime += dt;
            }

            CirclePlayer(dt);

            if (!GMInstance.CurrentShip || (GMInstance.CurrentShip.transform.position - transform.position).magnitude > (AggroRange * DeaggroRangeScale))
            {
                Aggroed = false;
                for (int i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i] != null)
                    {
                        Parts[i].Active = false;
                    }
                    else
                    {
                        Parts.RemoveAt(i);
                        --i;
                    }
                }
                BGL.Active = false;
            }
        }

        ////Move
        float moveDirection = 0.0f;

        float dotProduct = Vector3.Dot(transform.up, MoveDirection.normalized);
        if (dotProduct > 0.3)
        {
            if (Aggroed && (GMInstance.CurrentShip.transform.position - transform.position).magnitude <= (AggroRange * StrafeRangeScale))
                moveDirection = StrafeMoveScale;
            else
                moveDirection = 1.0f;
        }
        else if (dotProduct < -0.3)
        {
            moveDirection = -1.0f;
        }
        else
        {
            moveDirection = 0.0f;
        }

        if ((moveDirection > 0.0f && Acceleration < 0.0f)
            || (moveDirection < 0.0f && Acceleration > 0.0f)
            || moveDirection == 0.0f)
        {
            Acceleration = 0.0f;
        }

        if (moveDirection != 0.0f)
        {
            Acceleration += Jerk * moveDirection * Time.deltaTime;
            if (Aggroed && (GMInstance.CurrentShip.transform.position - transform.position).magnitude <= (AggroRange * StrafeRangeScale))
                Acceleration = Mathf.Clamp(Acceleration, -MaxAcceleration * StrafeMoveScale, MaxAcceleration * StrafeMoveScale);
            else
                Acceleration = Mathf.Clamp(Acceleration, -MaxAcceleration, MaxAcceleration);
        }

        Vector2 forward = new Vector2(transform.up.x, transform.up.y);
        forward *= Acceleration * Time.deltaTime;
        BossPhysics.velocity += forward;

        //Drag
        if (Aggroed && (GMInstance.CurrentShip.transform.position - transform.position).magnitude <= (AggroRange * StrafeRangeScale))
            BossPhysics.velocity += (Drag / StrafeMoveScale) * Time.deltaTime * -BossPhysics.velocity;
        else
            BossPhysics.velocity += Drag * Time.deltaTime * -BossPhysics.velocity;

        ////Turn
        Vector3 newDirection = Vector3.RotateTowards(transform.up, MoveDirection, TurnSpeed * dt, 1.0f);
        transform.rotation = Quaternion.LookRotation(transform.forward, newDirection);

        KeepInWorldSpace();
    }

    private void KeepInWorldSpace()
    {
        Vector2 position = transform.position;
        Vector2 velocity = BossPhysics.velocity;
        Vector2 WorldBounds = GMInstance.GetWorldBoundaries();
        float TurnStrength = 10.0f * Time.deltaTime;

        if (position.x < -WorldBounds.x)
        {
            velocity.x += TurnStrength;
            MoveDirection.x += TurnStrength;
        }
        else if (position.x > WorldBounds.x)
        {
            velocity.x -= TurnStrength;
            MoveDirection.x -= TurnStrength;
        }

        if (position.y < -WorldBounds.y)
        {
            velocity.y += TurnStrength;
            MoveDirection.y += TurnStrength;
        }
        else if (position.y > WorldBounds.y)
        {
            velocity.y -= TurnStrength;
            MoveDirection.y -= TurnStrength;
        }

        BossPhysics.velocity = velocity;
    }

    private void Patrol(float dt)
    {
        PatrolTimer -= dt;
        if (PatrolTimer <= 0.0f)
        {
            PatrolTimer = Random.Range(8.0f, 15.0f);
            float direction = Random.Range(-180.0f, 180.0f);
            MoveDirection = Quaternion.AngleAxis(direction, Vector3.forward) * transform.up;
            MoveDirection.z = 0.0f;
        }
    }

    private void CirclePlayer(float dt)
    {
        Vector3 toPlayer = (GMInstance.CurrentShip.transform.position - transform.position);
        float distance = toPlayer.magnitude;
        if (distance < (AggroRange * StrafeRangeScale))
        {
            float dot = Vector3.Dot(transform.right, toPlayer);
            int LoR = 0;
            if (dot == 0)
            {
                LoR = Random.Range(0, 2);
                if (LoR == 0)
                    LoR = -1;
            }
            else if (dot < 0)
            {
                LoR = 1;
            }
            else
            {
                LoR = -1;
            }

            Vector3 Strafe = new Vector3(LoR * toPlayer.y, -LoR * toPlayer.x, 0.0f);
            Strafe.Normalize();
            toPlayer.Normalize();
            MoveDirection = Strafe + (0.2f * toPlayer);
        }
        else
        {
            MoveDirection = new Vector3(toPlayer.x, toPlayer.y, 0.0f);
        }
    }


    private void SpawnExplosion()
    {
        if (ExplosionPoints.Count == 0)
        {
            Instantiate(ExplosionFinal, transform.position + new Vector3(0, 0, -0.5f), Quaternion.identity);
            StartCoroutine(GMInstance.GameCamera.CameraShake(1.0f));
            return;
        }
        Instantiate(Explosion, ExplosionPoints[0].transform.position + new Vector3(0, 0, -0.5f), Quaternion.identity);
        StartCoroutine(GMInstance.GameCamera.CameraShake(0.6f));
        ExplosionPoints.RemoveAt(0);
    }

    private void OnDeath(float time)
    {
        BGL.Active = false;
        for (int i = 0; i < ExplosionPoints.Count; i++)
        {
            Invoke("SpawnExplosion", time / (i + 2));
        }
        Invoke("SpawnExplosion", time);
        GMInstance.DisplayText.SetText("Boss Defeated!");
        GMInstance.DisplayText.CrossFadeAlpha(1.0f, 0.5f, true);
        GMInstance.TextTimer = time;
        Destroy(gameObject, time + 1.0f);
    }
}

/// Code from an academic project January to Febuary 2026 with a focus on Action Lists
/// We made a Card Game Interface where card logic and ui were handled using Action Lists.
/// Actions and easing were used to make the cards and ui feel smooth and natural.

public class ActionList
{
    public List<Action> actions = new List<Action>();

    public void UpdateList(float DtScaler)
    {
        float dt = Time.deltaTime * DtScaler;
        for (int i = 0; i < actions.Count; i++)
        {
            if (!actions[i].DecrementDelay(dt))
            {
                continue;
            }

            if (actions[i].starting)
            {
                actions[i].Start();
            }

            actions[i].Update(dt);

            if (actions[i].finished)
            {
                actions[i].Exit();
                actions.RemoveAt(i);
                --i;
                continue;
            }

            if (actions[i].blocking)
            {
                break;
            }

        }
    }

    public void Clear()
    {
        actions.Clear();
    }

    public void PushBack(Action action)
    {
        action.parent = this;
        actions.Add(action);
    }

    public int GetActionIndex(Action action)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] == action)
                return i;
        }
        return -1;
    }

    public bool IsListEmpty()
    {
        return actions.Count == 0;
    }

    public string GetDebugText(int actionLimit = 10)
    {
        string debugText = string.Empty;
        for (int i = 0; i < actions.Count; ++i)
        {
            if (i < actionLimit / 2 || i > actions.Count - (actionLimit / 2))
            {
                debugText += actions[i].DebugLine() + "\n";
            }
            else
            {
                debugText += $"... {actions.Count - i} Actions ...\n";
                i = actions.Count - i;
            }
        }
        return debugText;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager GMInstance;

    private StreamWriter DataStream;
    public GameObject DebugCanvas;
    private TextMeshProUGUI PerformaceText;

    [HideInInspector]
    public bool Paused = false;
    [HideInInspector]
    public bool AutomationMode = false;
    [HideInInspector]
    public float DtScale = 1.0f;

    private int ChildEnabled = 0;

    //Performance Data
    public int FramesWindow = 200;
    private List<float> PreviousFrameRates = new List<float>();
    public float WorstTimeCheck = 10.0f;
    private float MeanRate = 0.0f;
    private float MedianRate = 0.0f;
    private float WorstRate = 0.0f;

    private float Timer = 0.0f;

    private void Awake()
    {
        if (GMInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        GMInstance = this;

        DontDestroyOnLoad(gameObject);

        Directory.CreateDirectory(Application.dataPath + "\\Telemetry");

        string filename = DateTime.Now.ToString("MM-dd_tthh.mm.ss");

        DataStream = new StreamWriter($"{Application.dataPath}\\Telemetry\\Telemetry_{filename}.csv");

        DataStream.WriteLine("TelemetryData");
        DataStream.Write("Time, Frame Rate, Mean Frame Rate, Median Frame Rate, Worst Frame Rate");
        DataStream.Write(",CardsPlayed, HandsPlayed, Points");
        DataStream.Write(",Escape Presses, Resume Presses, Play Speed Presses, Hand Count Presses, Number of Hands Presses, Quit Presses\n");
    }

    void Start()
    {
        GameObject cardManager = GameObject.Find("CardManager");
        if (cardManager != null)
        {
            cardManager.GetComponent<CardManager>().DebugText = DebugCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        }
        GameObject uiManager = GameObject.Find("UIManager");
        if (uiManager != null)
        {
            uiManager.GetComponent<UIManager>().DebugText = DebugCanvas.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        }
        PerformaceText = DebugCanvas.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        DebugCanvas.SetActive(false);
    }

    void Update()
    {
        Timer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugCanvas.SetActive(!DebugCanvas.activeSelf);
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            AutomationMode = !AutomationMode;
        }

        float frameRate = 1.0f / Time.deltaTime;

        PreviousFrameRates.Add(frameRate);

        MeanRate = 0.0f;

        List<float> SortedRates = new List<float>();
        int startIndex = Mathf.Max(0, PreviousFrameRates.Count - FramesWindow);
        for (int i = startIndex; i < PreviousFrameRates.Count; i++)
        {
            MeanRate += PreviousFrameRates[i];
            SortedRates.Add(PreviousFrameRates[i]);
        }
        MeanRate /= Mathf.Min(PreviousFrameRates.Count, FramesWindow);

        SortedRates.Sort();
        int medianIndex = (int)(Mathf.Min(SortedRates.Count, FramesWindow) * 0.5f);

        MedianRate = SortedRates[medianIndex];

        if (DebugCanvas.activeSelf)
        {
            PerformaceText.SetText($"Frame Rate: {frameRate:F2}\nMean Rate: {MeanRate:F2}\nMedian Rate: {MedianRate:F2}\nWorst Rate: {WorstRate:F2}");
        }

        if (Timer >= WorstTimeCheck)
        {
            Timer = 0.0f;
            WorstRate = Mathf.Min(PreviousFrameRates.ToArray());

            WriteTelemetry();

            PreviousFrameRates.Clear();
        }
    }

    private void WriteTelemetry()
    {
        //Write General Game Data
        DataStream.Write($"{Time.time:F3}, {(PreviousFrameRates[PreviousFrameRates.Count - 1]):F3}, {MeanRate:F2}, {MedianRate:F2}, {WorstRate:F2}");
        //Write Card Data
        DataStream.Write($",{CMInstance.CardsPlayed}, {CMInstance.HandsPlayed}, <{CMInstance.HandPoints[0]}; {CMInstance.HandPoints[1]}; {CMInstance.HandPoints[2]}; {CMInstance.HandPoints[3]}>");
        //Write UI Data
        DataStream.Write($",{UMInstance.TimesEscapePressed}");
        for (int i = 0; i < UMInstance.TimesButtonPressed.Length; i++)
        {
            DataStream.Write($",{UMInstance.TimesButtonPressed[i]}");
        }
        DataStream.Write("\n");
    }

    public void SwitchTextEnabled()
    {
        if (ChildEnabled == 0)
        {
            DebugCanvas.transform.GetChild(ChildEnabled).gameObject.SetActive(false);
            ChildEnabled = 1;
            DebugCanvas.transform.GetChild(ChildEnabled).gameObject.SetActive(true);
        }
        else
        {
            DebugCanvas.transform.GetChild(ChildEnabled).gameObject.SetActive(false);
            ChildEnabled = 0;
            DebugCanvas.transform.GetChild(ChildEnabled).gameObject.SetActive(true);
        }
    }

    public void OnDestroy()
    {
        WriteTelemetry();
        DataStream.Close();
    }
}

public class CardManager : MonoBehaviour
{
    public static CardManager CMInstance;
    [HideInInspector]
    public ActionList CardActionList;

    [HideInInspector]
    public List<GameObject> Cards;

    [HideInInspector]
    public List<GameObject>[] Hands = { new List<GameObject>(), new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };

    [HideInInspector]
    public TextMeshProUGUI[] HandText = { null, null, null, null };
    [HideInInspector]
    public int[] HandPoints = { 0, 0, 0, 0 };
    [HideInInspector]
    public string[] HandNames = { "Player", "Other 1", "Other 2", "Other 3" };

    private Vector3[] HandPositions = { new(0, -3.2f, 0), new(-7.5f, 0, 0), new(0, 3.2f, 0), new(7.5f, 0, 0) };
    private Vector3[] HandUIOffsets = { new(-2.8f, 1.5f, 0), new(1.5f, 2.5f, 0), new(2.8f, -1.5f, 0), new(-1.5f, -2.5f, 0) };
    private float[] HandRotations = { 0.0f, -90.0f, 180.0f, -270.0f };

    private GameObject CardPrefab;
    private GameObject HandUIPrefab;
    private GameObject PointPlusPrefab;

    private bool shuffled = false;

    [HideInInspector]
    public TextMeshProUGUI DebugText;

    [HideInInspector]
    public int CardsPlayed = 0;
    [HideInInspector]
    public int HandsPlayed = 0;

    public float Drift = 0.05f;
    public float Twist = 5.0f;

    public int CardsInDeck = 52;
    public int HandSize = 7;
    public int NumHands = 4;

    public int NumberOfHands
    {
        get { return NumHands; }
        set { NumHands = Mathf.Clamp(value, 1, 4); }
    }

    public int CardsPerHand
    {
        get { return HandSize; }
        set { HandSize = Mathf.Clamp(value, 1, CardsInDeck / Hands.Length); }
    }

    public Vector3 deckPosition = new Vector3(5f, -4f, 0.0f);
    public Vector3 deckOffsets = new Vector3(-0.005f, 0.004f, -0.05f);
    public Vector3 discardPosition = new Vector3(-5f, 4f, 0.0f);
    public float MaxCardArcAngle = 180.0f;
    public float AngleBetweenCards = 5f;
    public float hand_radius = 6f;
    public int DebugListSize = 25;

    private void Awake()
    {
        if (CMInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        CMInstance = this;

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        CardActionList = new ActionList();

        Cards = new List<GameObject>();

        CardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Card");

        //Create each card and move it to the proper position in deck format
        for (int i = 0; i < CardsInDeck; i++)
        {
            string cardName = "Card";
            Vector3 startPos = deckPosition + (deckOffsets * i);
            GameObject Card = Instantiate(CardPrefab, this.transform);
            Card.transform.position = startPos;
            Cards.Add(Card);

            TextMeshProUGUI suit = Card.transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI suit1 = Card.transform.GetChild(0).GetChild(3).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI value = Card.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI value1 = Card.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
            switch (i / 13)
            {
                case 0:
                    cardName += "\u2665";
                    suit.SetText("\u2665");
                    suit.color = Color.red;
                    value.color = Color.red;
                    suit1.SetText("\u2665");
                    suit1.color = Color.red;
                    value1.color = Color.red;
                    break;
                case 1:
                    cardName += "\u2660";
                    suit.SetText("\u2660");
                    suit.color = Color.black;
                    value.color = Color.black;
                    suit1.SetText("\u2660");
                    suit1.color = Color.black;
                    value1.color = Color.black;
                    break;
                case 2:
                    cardName += "\u2666";
                    suit.SetText("\u2666");
                    suit.color = Color.red;
                    value.color = Color.red;
                    suit1.SetText("\u2666");
                    suit1.color = Color.red;
                    value1.color = Color.red;
                    break;
                case 3:
                    cardName += "\u2663";
                    suit.SetText("\u2663");
                    suit.color = Color.black;
                    value.color = Color.black;
                    suit1.SetText("\u2663");
                    suit1.color = Color.black;
                    value1.color = Color.black;
                    break;
            }

            switch ((i % 13))
            {
                case 0:
                    cardName += "A";
                    value.SetText("A");
                    value1.SetText("A");
                    break;
                case 10:
                    cardName += "J";
                    value.SetText("J");
                    value1.SetText("J");
                    break;
                case 11:
                    cardName += "Q";
                    value.SetText("Q");
                    value1.SetText("Q");
                    break;
                case 12:
                    cardName += "K";
                    value.SetText("K");
                    value1.SetText("K");
                    break;
                default:
                    cardName += ((i % 13) + 1).ToString();
                    value.SetText(((i % 13) + 1).ToString());
                    value1.SetText(((i % 13) + 1).ToString());
                    break;
            }

            Card.name = cardName;
        }

        ShuffleCards();

        shuffled = true;

        HandUIPrefab = Resources.Load<GameObject>("Prefabs/UI/BaseHandUI");

        for (int h = 0; h < Hands.Length; h++)
        {
            GameObject HandUI = Instantiate(HandUIPrefab, HandPositions[h] + HandUIOffsets[h], Quaternion.identity);
            HandUI.name = HandNames[h];

            HandText[h] = HandUI.GetComponentInChildren<TextMeshProUGUI>();
            HandText[h].SetText(HandNames[h] + ":\n" + HandPoints[h].ToString());
        }

        PointPlusPrefab = Resources.Load<GameObject>("Prefabs/UI/PointPlus");
    }


    private void ShuffleCards()
    {

        float cardWidth = CardPrefab.transform.lossyScale.x;
        float startDelay = 0.15f;
        float moveDuration = 0.25f;

        for (int c = 0; c < 2; ++c)
        {
            for (int i = 0; i < Cards.Count; i++)
            {
                Vector3 startPos = deckPosition + (deckOffsets * i);

                float targetX = (cardWidth * 0.5f) + (cardWidth * Random.Range(-1.4f, 1.25f));
                Vector3 newPos = new(startPos.x + targetX, startPos.y + Random.Range(0.0f, 0.2f), startPos.z);

                float delayOffset = i / 500.0f;
                CardActionList.PushBack(new MoveTo(Cards[i], newPos, moveDuration, Easing.In, startDelay + delayOffset));
                CardActionList.PushBack(new MoveTo(Cards[i], startPos, moveDuration, Easing.Out, moveDuration + startDelay + delayOffset));
            }
            CardActionList.PushBack(new Block(1.0f + startDelay + (Cards.Count / 500.0f)));
            CardActionList.PushBack(new ShuffleCards());
        }
    }

    private void DealCards()
    {
        float cardWidth = CardPrefab.transform.lossyScale.x;
        float cardHeight = CardPrefab.transform.lossyScale.y;
        float startDelay = 0.1f;
        float moveDuration = 0.4f;
        float movePercentDelay = 0.35f;
        float movePercentRotation = 0.8f;
        float DealDrift = Drift * 5.0f;

        for (int i = 0; i < HandSize; ++i)
        {
            float delayOffset = moveDuration * movePercentDelay * NumHands * i;
            float totalDelay = startDelay + delayOffset;
            int index = Cards.Count - (i * NumHands) - 1;
            for (int h = 0; h < NumHands; ++h)
            {
                Hands[h].Add(Cards[index - h]);

                Vector3 newPos = HandPositions[h];
                newPos += new Vector3(Random.Range(-DealDrift, DealDrift), Random.Range(-DealDrift, DealDrift), (deckOffsets.z * i));

                CardActionList.PushBack(new MoveTo(Cards[index - h], newPos, moveDuration, Easing.In, totalDelay));
                CardActionList.PushBack(new Rotate(Cards[index - h], Random.Range(-720, 720), moveDuration * movePercentRotation, Easing.In, totalDelay + (moveDuration * (1.0f - movePercentRotation))));
                totalDelay += moveDuration * movePercentDelay;
            }
        }

        CardActionList.PushBack(new Block(2.0f + startDelay + (Cards.Count / 500.0f)));
    }


    private void CleanUpHands()
    {
        float moveDuration = 0.9f;

        for (int h = 0; h < NumHands; h++)
        {
            //Grab the relevant player data
            List<GameObject> player_deck = Hands[h];
            float player_deck_count = player_deck.Count;

            for (int i = 0; i < player_deck_count; i++)
            {
                GameObject card = player_deck[i];

                //Move cards to middle of hand and clean up
                CardActionList.PushBack(new MoveTo(card, HandPositions[h], moveDuration, Easing.FastIn));
                CardActionList.PushBack(new RotateTo(card, HandRotations[h], moveDuration, Easing.In));
                CardActionList.PushBack(new FadeUI(HandText[h], 1.0f, moveDuration * 1.5f, moveDuration));
            }
        }
    }

    private void RepositionHands(bool flipCards)
    {
        float moveDuration = 0.6f;

        for (int h = 0; h < NumHands; h++)
        {
            float handSize = Hands[h].Count;

            float arcAngle = Mathf.Min(MaxCardArcAngle, handSize * AngleBetweenCards);

            float step = 0;
            if (handSize > 1)
            {
                step = arcAngle / (handSize - 1);
            }

            float startAngle = (-arcAngle) / 2f;
            for (int i = 0; i < handSize; i++)
            {
                GameObject card = Hands[h][i];

                //Calculate the card's angle based on its index
                float angle = startAngle + (step * i);

                float radians = Mathf.Deg2Rad * angle;

                Vector3 positionOffset = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), -i * .01f) * hand_radius;

                Vector3 newPosition = HandPositions[h] + (Quaternion.Euler(0, 0, HandRotations[h]) * positionOffset) + (HandPositions[h].normalized * hand_radius);
                newPosition += new Vector3(Random.Range(-Drift, Drift), Random.Range(-Drift, Drift));

                CardActionList.PushBack(new MoveTo(card, newPosition, moveDuration, Easing.FastIn, moveDuration * 1.5f + (0.01f * i)));

                Quaternion newRotation = Quaternion.Euler(0, 0, HandRotations[h]) * Quaternion.Euler(0, 0, -angle);
                newRotation *= Quaternion.Euler(0, 0, Random.Range(-Twist, Twist));

                CardActionList.PushBack(new RotateTo(card, newRotation, moveDuration, Easing.In, moveDuration * 1.5f + (0.01f * i)));

                //Make sure the cards are flipped over for the player
                if (h == 0 && flipCards)
                {
                    CardActionList.PushBack(new Flip(card, moveDuration, Easing.FastOut, moveDuration * 1.5f + (0.01f * i)));
                }
            }
        }
        CardActionList.PushBack(new Block(0.5f));
    }

    private void DeckCards()
    {

        float startDelay = 0.2f;
        float moveDuration = 0.6f;

        for (int i = 0; i < Cards.Count; i++)
        {
            Vector3 newPos = deckPosition + new Vector3((deckOffsets.x * i), (deckOffsets.y * i), (deckOffsets.z * i));

            float delayOffset = i / 100.0f;
            if (Cards[i].transform.GetChild(0).gameObject.activeSelf)
            {
                CardActionList.PushBack(new Flip(Cards[i], moveDuration, Easing.Linear, startDelay + delayOffset));
            }
            CardActionList.PushBack(new MoveTo(Cards[i], newPos, moveDuration, Easing.Out, startDelay + delayOffset));
            CardActionList.PushBack(new RotateTo(Cards[i], 0, moveDuration, Easing.Out, startDelay + delayOffset));
        }
        for (int i = 0; i < 4; i++)
        {
            CardActionList.PushBack(new FadeUI(HandText[i], 0.0f, moveDuration, startDelay));
            HandPoints[i] = 0;
            CardActionList.PushBack(new SetText(HandText[i], HandNames[i] + ":\n" + HandPoints[i], moveDuration + startDelay));
        }
        CardActionList.PushBack(new Block(3.0f + startDelay + (Cards.Count / 500.0f)));
    }

    private void PlaySingleCard(int hand, int cardIndex)
    {
        float moveDuration = 0.5f;

        GameObject Card = Hands[hand][cardIndex];
        ++CardsPlayed;

        Hands[hand].RemoveAt(cardIndex);

        Vector3 direction = (-HandPositions[hand]);
        direction.Normalize();
        Vector3 playPositon = HandPositions[hand] + (direction * 2.0f) + new Vector3(Random.Range(-Drift, Drift), Random.Range(-Drift, Drift));

        if (hand != 0)
        {
            CardActionList.PushBack(new Flip(Card, 0.3f));
        }

        CardActionList.PushBack(new MoveTo(Card, playPositon, moveDuration, Easing.In));
        CardActionList.PushBack(new RotateTo(Card, HandRotations[hand] + Random.Range(-Twist, Twist), moveDuration, Easing.In));

        //MoveTo to the center
        CardActionList.PushBack(new MoveTo(Card, Vector3.zero + new Vector3(Random.Range(-Drift, Drift), Random.Range(-Drift, Drift), -0.01f * (hand + 1) * (cardIndex + 1)), moveDuration * 0.8f, Easing.In, moveDuration * 2.0f));
        CardActionList.PushBack(new Rotate(Card, Random.Range(-Twist, Twist) * 10.0f, moveDuration * 0.5f, Easing.In, moveDuration * 2.0f));

        //"Discard" card
        CardActionList.PushBack(new MoveTo(Card, discardPosition + new Vector3(Random.Range(-Drift, Drift), Random.Range(-Drift, Drift), -0.01f * hand * cardIndex), moveDuration, Easing.In, moveDuration * 3.0f));
        CardActionList.PushBack(new Rotate(Card, Random.Range(-Twist, Twist) * 2.0f, moveDuration, Easing.In, moveDuration * 3.0f));
    }

    private void IncrementRandomHandPoint()
    {
        float delay = 0.5f;
        float plusTime = 1.2f;

        int index = Random.Range(0, NumberOfHands);

        ++HandPoints[index];

        CardActionList.PushBack(new SetText(HandText[index], HandNames[index] + ":\n" + HandPoints[index], delay));

        GameObject Plus = Instantiate(PointPlusPrefab, HandPositions[index] + HandUIOffsets[index], Quaternion.identity);
        Plus.transform.position = Plus.transform.position + new Vector3(0, 0, -5f);

        CardActionList.PushBack(new FadeUI(Plus.GetComponentInChildren<TextMeshProUGUI>(), 1f, 0.001f, delay, Easing.Out));
        CardActionList.PushBack(new MoveTo(Plus, Plus.transform.position + new Vector3(1.0f, 0.25f), plusTime, Easing.In, delay));
        CardActionList.PushBack(new FadeUI(Plus.GetComponentInChildren<TextMeshProUGUI>(), 0f, plusTime, delay, Easing.In));

        GameObject.Destroy(Plus, plusTime + delay);
    }

    private void PlayCards()
    {
        if (GMInstance.AutomationMode && UMInstance.ButtonActionList.IsListEmpty())
        {
            for (int h = 0; h < NumHands; h++)
            {
                PlaySingleCard(h, Random.Range(0, Hands[h].Count));
            }
            IncrementRandomHandPoint();
            RepositionHands(false);
            CardActionList.PushBack(new QueueUIAutomation(0.8f));
            ++HandsPlayed;
        }
        else
        {
            Vector3 mousePositionWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            List<GameObject> playerHand = Hands[0];
            for (int i = playerHand.Count - 1; i >= 0; i--)
            {
                BoxCollider2D collider = playerHand[i].GetComponent<BoxCollider2D>();
                //If the mouse is within the card
                if (collider && collider.OverlapPoint(new Vector2(mousePositionWorld.x, mousePositionWorld.y)))
                {
                    //If the mouse is clicked
                    if (Input.GetMouseButtonDown(0))
                    {
                        PlaySingleCard(0, i);
                        for (int h = 1; h < NumHands; h++)
                        {
                            PlaySingleCard(h, Random.Range(0, Hands[h].Count));
                        }
                        IncrementRandomHandPoint();
                        RepositionHands(false);
                        ++HandsPlayed;
                        return;
                    }
                }
            }
        }
    }

    private bool AllHandsEmpty()
    {
        bool handsEmpty = (Hands[0].Count == 0);
        for (int i = 1; i < NumHands; ++i)
        {
            handsEmpty = (handsEmpty && (Hands[i].Count == 0));
        }
        return handsEmpty;
    }

    void Update()
    {
        if (!GMInstance.Paused && UMInstance.ButtonActionList.IsListEmpty())
        {
            if (CardActionList.IsListEmpty())
            {
                if (AllHandsEmpty())
                {
                    if (shuffled == false)
                    {
                        DeckCards();

                        ShuffleCards();

                        shuffled = true;
                    }
                    else
                    {
                        DealCards();

                        CleanUpHands();

                        RepositionHands(true);

                        shuffled = false;
                    }
                }
                else
                {
                    PlayCards();
                }
            }
            CardActionList.UpdateList(GMInstance.DtScale);
            if (DebugText)
            {
                if (GMInstance.DebugCanvas.activeSelf && DebugText.gameObject.activeSelf)
                {
                    DebugText.SetText("CardManager List:\n" + CardActionList.GetDebugText(DebugListSize));
                }
            }
        }

    }
}

public class UIManager : MonoBehaviour
{
    public static UIManager UMInstance;

    private void Awake()
    {
        if (UMInstance != null)
        {
            Destroy(gameObject);
            return;
        }

        UMInstance = this;

        DontDestroyOnLoad(gameObject);
    }

    [HideInInspector]
    public ActionList ButtonActionList;

    [HideInInspector]
    public List<GameObject> Buttons;
    private string[] ButtonLabels = { "Resume", "PlaySpeed", "HandSize", "NumberOfHands", "Quit" };
    private bool ResetCardsFlag = false;

    private GameObject ButtonPrefab;

    [HideInInspector]
    public TextMeshProUGUI DebugText;

    [HideInInspector]
    public int TimesEscapePressed = 0;

    [HideInInspector]
    public int[] TimesButtonPressed = { 0, 0, 0, 0, 0 };

    public float Drift = 0.05f;
    public float Twist = 5.0f;

    //Pause Menu Values
    public Vector3 buttonCenterPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 buttonOffset = new Vector3(0.0f, 10.0f, 0.0f);
    public float MoveInDuration = 0.3f;
    public float SpreadDuration = 0.4f;
    public float MaxArcAngle = 180.0f;
    public float MaxAngleBetweenButtons = 10f;
    public float SpreadRadius = 15f;
    public int DebugListSize = 25;

    void Start()
    {
        ButtonActionList = new ActionList();

        Buttons = new List<GameObject>();

        ButtonPrefab = Resources.Load<GameObject>("Prefabs/UI/BaseButton");

        Vector3 startPos = buttonCenterPosition + buttonOffset;

        for (int i = 0; i < ButtonLabels.Length; i++)
        {
            GameObject NewButton = Instantiate(ButtonPrefab, this.transform);
            NewButton.transform.position = startPos;
            NewButton.name = ButtonLabels[i];
            NewButton.GetComponentInChildren<Button>().interactable = false;
            Buttons.Add(NewButton);
        }
        Buttons[0].GetComponentInChildren<Button>().onClick.AddListener(Resume);
        Buttons[0].GetComponentInChildren<TextMeshProUGUI>().SetText(ButtonLabels[0]);

        Buttons[1].GetComponentInChildren<TextMeshProUGUI>().SetText(ButtonLabels[1] + ":\n" + GMInstance.DtScale.ToString());
        Buttons[1].GetComponentInChildren<Button>().onClick.AddListener(PlaySpeed);

        Buttons[2].GetComponentInChildren<TextMeshProUGUI>().SetText(ButtonLabels[2] + ":\n" + CMInstance.CardsPerHand.ToString());
        Buttons[2].GetComponentInChildren<Button>().onClick.AddListener(HandSize);

        Buttons[3].GetComponentInChildren<TextMeshProUGUI>().SetText(ButtonLabels[3] + ":\n" + CMInstance.NumberOfHands.ToString());
        Buttons[3].GetComponentInChildren<Button>().onClick.AddListener(NumberOfHands);

        Buttons[4].GetComponentInChildren<Button>().onClick.AddListener(Quit);
        Buttons[4].GetComponentInChildren<TextMeshProUGUI>().SetText(ButtonLabels[4]);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ++TimesEscapePressed;
            if (GMInstance.Paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        ButtonActionList.UpdateList(GMInstance.DtScale);

        if (DebugText)
        {
            if (GMInstance.DebugCanvas.activeSelf && DebugText.gameObject.activeSelf)
            {
                DebugText.SetText("UIManger List:\n" + ButtonActionList.GetDebugText(DebugListSize));
            }
        }
    }

    public void Pause()
    {
        ButtonActionList.Clear();

        GMInstance.Paused = true;

        float numButtons = Buttons.Count;

        float arcAngle = Mathf.Min(MaxArcAngle, numButtons * MaxAngleBetweenButtons);

        float step = 0;
        if (numButtons > 1)
        {
            step = arcAngle / (numButtons - 1);
        }

        float startAngle = (-arcAngle) / 2f;

        for (int i = 0; i < numButtons; i++)
        {
            GameObject button = Buttons[i];

            ButtonActionList.PushBack(new MoveTo(button, buttonCenterPosition, MoveInDuration, Easing.In));

            float angle = startAngle + (step * i);
            float radians = Mathf.Deg2Rad * angle;

            Vector3 positionOffset = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), -i * .01f) * SpreadRadius;

            Vector3 newPosition = buttonCenterPosition + positionOffset + (Vector3.down * (SpreadRadius * 1.0f));
            newPosition += new Vector3(Random.Range(-Drift, Drift), Random.Range(-Drift, Drift));

            ButtonActionList.PushBack(new MoveTo(button, newPosition, SpreadDuration, Easing.FastIn, MoveInDuration));

            Quaternion newRotation = Quaternion.Euler(0, 0, -angle);
            newRotation *= Quaternion.Euler(0, 0, Random.Range(-Twist, Twist));

            ButtonActionList.PushBack(new RotateTo(button, newRotation, SpreadDuration, Easing.In, MoveInDuration));

            ButtonActionList.PushBack(new SetClickable(button.GetComponentInChildren<Button>(), true, MoveInDuration + SpreadDuration));
        }
        ButtonActionList.PushBack(new Block());
    }

    public static void Resume()
    {
        UMInstance.ButtonActionList.Clear();

        GMInstance.Paused = false;

        ++UMInstance.TimesButtonPressed[0];

        if (UMInstance.ResetCardsFlag)
        {
            UMInstance.ResetCardsFlag = false;
            
            //Clear the action list
            CMInstance.CardActionList.Clear();
            
            //Empty Hands
            for (int i = 0; i < CMInstance.Hands.Length; i++)
            {
                CMInstance.Hands[i].Clear();
            }
        }

        Vector3 newPos = UMInstance.buttonCenterPosition + UMInstance.buttonOffset;
        float moveCenterDuration = 0.5f;

        for (int i = 0; i < UMInstance.Buttons.Count; i++)
        {
            UMInstance.ButtonActionList.PushBack(new SetClickable(UMInstance.Buttons[i].GetComponentInChildren<Button>(), false));
            UMInstance.ButtonActionList.PushBack(new MoveTo(UMInstance.Buttons[i], UMInstance.buttonCenterPosition, moveCenterDuration, Easing.FastIn));
            UMInstance.ButtonActionList.PushBack(new RotateTo(UMInstance.Buttons[i], 0, moveCenterDuration, Easing.In));
            UMInstance.ButtonActionList.PushBack(new MoveTo(UMInstance.Buttons[i], newPos, UMInstance.MoveInDuration, Easing.In, moveCenterDuration));
        }
    }

    public static void PlaySpeed()
    {
        switch (GMInstance.DtScale)
        {
            case 1.0f:
                GMInstance.DtScale = 2.0f;
                break;
            case 2.0f:
                GMInstance.DtScale = 3.0f;
                break;
            case 3.0f:
                GMInstance.DtScale = 0.5f;
                break;
            case 0.5f:
                GMInstance.DtScale = 1.0f;
                break;
        }
        UMInstance.Buttons[1].GetComponentInChildren<TextMeshProUGUI>().SetText(UMInstance.ButtonLabels[1] + ":\n" + GMInstance.DtScale.ToString());
        EventSystem.current.SetSelectedGameObject(null);
        ++UMInstance.TimesButtonPressed[1];
    }

    public static void HandSize()
    {
        UMInstance.ResetCardsFlag = true;
        int oldHandSize = CMInstance.CardsPerHand;
        //Increment the cards per hand
        CMInstance.CardsPerHand = oldHandSize + 1;
        //Reset to min if we've hit max cards per hand
        if (CMInstance.CardsPerHand == oldHandSize)
        {
            CMInstance.CardsPerHand = 0;
        }
        UMInstance.Buttons[2].GetComponentInChildren<TextMeshProUGUI>().SetText(UMInstance.ButtonLabels[2] + ":\n" + CMInstance.CardsPerHand.ToString());
        EventSystem.current.SetSelectedGameObject(null);
        ++UMInstance.TimesButtonPressed[2];
    }

    public static void NumberOfHands()
    {
        UMInstance.ResetCardsFlag = true;
        int oldNumHands = CMInstance.NumberOfHands;
        //Increment the number of hands
        CMInstance.NumberOfHands = oldNumHands + 1;
        //Reset to min if we've hit max num hands
        if (CMInstance.NumberOfHands == oldNumHands)
        {
            CMInstance.NumberOfHands = 0;
        }
        UMInstance.Buttons[3].GetComponentInChildren<TextMeshProUGUI>().SetText(UMInstance.ButtonLabels[3] + ":\n" + CMInstance.NumberOfHands.ToString());
        EventSystem.current.SetSelectedGameObject(null);
        ++UMInstance.TimesButtonPressed[3];
    }

    public static void Quit()
    {
        ++UMInstance.TimesButtonPressed[4];
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}

/// Code from a Global Game Jam in November 2025
/// Focus was on a dynamic tile map that is changed by user input
/// and could also be used for enemy pathing.

public class TileMap : MonoBehaviour
{

    static public TileMap TileMapObject;

    private const int _MapHeight = 8;
    private const int _MapWidth = 10;

    public GameObject[] Tiles = new GameObject[8 * 10];
    public float TileSize = 1.0f;
    public float TileDistance = 1.0f;
    public float TimeToDelete = 0.3f;
    [HideInInspector]
    public float _Timer = 0.0f;

    public Transform BlankTilesParent;
    public Transform BackgroundTilesParent;

    private BoxCollider2D _BoxCollider;

    private GameObject _BlankTile = null;
    [HideInInspector]
    public GameObject PrefabToPlace = null;
    [HideInInspector]
    public int PrefabCost = 0;
    [HideInInspector]
    public bool isValidPath = false;
    [HideInInspector]
    public bool canPlace = true;

    //Tile that represents leftmost and right most tiles
    [HideInInspector]
    public GameObject PlayerTile = null;
    [HideInInspector]
    public GameObject EnemyTile = null;

    [SerializeField] private AudioClip _audioClip;

    private void Awake()
    {
        if(TileMapObject != null && TileMapObject != this)
        {
            Destroy(gameObject);
            return;
        }

        TileMapObject = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PrefabCost = 0;
        PrefabToPlace = null;

        _BlankTile = Resources.Load<GameObject>("Prefabs/Tiles/Blank");
        _BlankTile.transform.localScale = new Vector3(TileSize, TileSize, 1.0f);

        //Set the size of the collider for seeing if the mouse in within the tile map
        _BoxCollider = gameObject.GetComponent<BoxCollider2D>();
        Vector2 colliderSize = new Vector2(TileDistance * _MapWidth, TileDistance * _MapHeight);
        _BoxCollider.size = colliderSize;
        _BoxCollider.offset = new Vector2((colliderSize.x / 2.0f) - (TileDistance / 2.0f), (colliderSize.y / 2.0f) - (TileDistance / 2.0f));

        string filePath = "Sprites/BlankTiles";
        Sprite[] sprites = Resources.LoadAll<Sprite>(filePath);
        //Fill the map with blank tiles
        for (int x = 0; x < _MapWidth; x++)
        {
            for (int y = 0; y < _MapHeight; y++)
            {
                int spriteIndex = UnityEngine.Random.Range(0, sprites.Length);
                GameObject background = Spawn(_BlankTile, x, y);
                background.GetComponent<SpriteRenderer>().sprite = sprites[spriteIndex];
                background.transform.parent = BlankTilesParent;
                background.transform.localPosition = new Vector3(background.transform.localPosition.x, background.transform.localPosition.y, 0.0f);
            }
        }

        filePath = "Sprites/BorderTiles";
        sprites = Resources.LoadAll<Sprite>(filePath);
        for (int x = -1; x <= _MapWidth; x++)
        {
            int spriteIndex = UnityEngine.Random.Range(0, sprites.Length);
            GameObject background = Spawn(_BlankTile, x, -1);
            background.GetComponent<SpriteRenderer>().sprite = sprites[spriteIndex];
            background.transform.parent = BackgroundTilesParent;
            background.transform.localPosition = new Vector3(background.transform.localPosition.x, background.transform.localPosition.y, 0.0f);

            spriteIndex = UnityEngine.Random.Range(0, sprites.Length);
            background = Spawn(_BlankTile, x, _MapHeight);
            background.GetComponent<SpriteRenderer>().sprite = sprites[spriteIndex];
            background.transform.parent = BackgroundTilesParent;
            background.transform.localPosition = new Vector3(background.transform.localPosition.x, background.transform.localPosition.y, 0.0f);
        }
        for (int y = 0; y < _MapHeight; y++)
        {
            int spriteIndex = UnityEngine.Random.Range(0, sprites.Length);
            GameObject background = Spawn(_BlankTile, -1, y);
            background.GetComponent<SpriteRenderer>().sprite = sprites[spriteIndex];
            background.transform.parent = BackgroundTilesParent;
            background.transform.localPosition = new Vector3(background.transform.localPosition.x, background.transform.localPosition.y, 0.0f);

            spriteIndex = UnityEngine.Random.Range(0, sprites.Length);
            background = Spawn(_BlankTile, _MapWidth, y);
            background.GetComponent<SpriteRenderer>().sprite = sprites[spriteIndex];
            background.transform.parent = BackgroundTilesParent;
            background.transform.localPosition = new Vector3(background.transform.localPosition.x, background.transform.localPosition.y, 0.0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!canPlace || Instance.GamePhaseManager.CurrentPhase != GamePhase.Build) return;
        Vector3 mousePositionWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (_BoxCollider.OverlapPoint(new(mousePositionWorld.x, mousePositionWorld.y)))
        {
            Vector2 localPosition = new Vector2(mousePositionWorld.x + (TileDistance / 2.0f), mousePositionWorld.y + (TileDistance / 2.0f)) - new Vector2(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y);

            Vector2Int mouseGridPosition = new Vector2Int((int)(localPosition.x / TileDistance), (int)(localPosition.y / TileDistance));

            if(Input.GetMouseButton(0)) // LMB
            {
                if (PrefabToPlace != null || !(Instance.PauseManager.IsPaused))
                {
                    SpawnTile(PrefabToPlace, mouseGridPosition.x, mouseGridPosition.y); 
                }
            }
            else if(Input.GetMouseButton(1)) // RMB
            {
                _Timer += Time.deltaTime;
                if (_Timer >= TimeToDelete)
                {
                    _Timer = TimeToDelete;
                    SpawnTile(null, mouseGridPosition.x, mouseGridPosition.y);
                }
            }
            else
            {
                _Timer = 0.0f;
            }
        }
        else if (Input.GetMouseButton(1) || Input.GetMouseButton(0))
        {
            Instance.UI_HUDController._shopController._infoPanelController.EndPlacement();
            PrefabToPlace = null;
            PrefabCost = 0;
        }
    }
    
    public List<Vector3> GetPath(Vector3 start, Vector3 end)
    {

        GameObject startTile = GetTile(GetGridPosition(start));
        if (startTile && startTile.GetComponent<Path>())
        {
            startTile.GetComponent<Path>().Parent = null;
            List<Path> openList = new List<Path>();
            openList.Add(startTile.GetComponent<Path>());
            while (openList.Count > 0)
            {
                Path node = openList[0];
                openList.RemoveAt(0);
                if(node.GridPosition == GetGridPosition(end))
                {
                    List<Vector3> path = new();
                    Path temp = node;
                    while(temp != null)
                    {
                        path.Add(GetWorldPosition(temp.GridPosition));
                        temp = temp.Parent;
                    }
                    path.Reverse();
                    return path;
                }
                for (int i = 0; i < node.Neighbors.Count; i++)
                {
                    if(node.Neighbors[i] != node.Parent)
                    {
                        Path neighbor = node.Neighbors[i];
                        neighbor.Parent = node;
                        openList.Add(neighbor);
                    }
                }
            }
        }
        return new List<Vector3>();
    }
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector2 localPosition = new Vector2(worldPosition.x + (TileDistance / 2.0f), worldPosition.y + (TileDistance / 2.0f)) - new Vector2(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y);
        Vector2Int gridPosition = new Vector2Int((int)(localPosition.x / TileDistance),(int)(localPosition.y / TileDistance));
        if (gridPosition.x < 0 ||  gridPosition.y < 0
            || gridPosition.x > _MapWidth || gridPosition.y > _MapHeight)
        {
            return new Vector2Int(-1, -1);
        }
        return gridPosition;
    }

    public Vector3 GetWorldPosition(Vector2Int GridPos)
    {
        return Tiles[(GridPos.y * _MapWidth) + GridPos.x].transform.position;
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return Tiles[(y * _MapWidth) + x].transform.position;
    }

    public GameObject GetTile(int x, int y)
    {
        if (x < 0 || x >= _MapWidth || y < 0 || y >= _MapHeight)
            return null;
        return Tiles[(y * _MapWidth) + x];
    }

    public GameObject GetTile(Vector2Int GridPos)
    {
        if (GridPos.x < 0 || GridPos.x >= _MapWidth || GridPos.y < 0 || GridPos.y >= _MapHeight)
            return null;
        return Tiles[(GridPos.y * _MapWidth) + GridPos.x];
    }

    private GameObject Spawn(GameObject obj, float x, float y)
    {
        GameObject newObject = Instantiate(obj, new Vector3(gameObject.transform.localPosition.x + (x * TileDistance), gameObject.transform.localPosition.y + (y * TileDistance), 0.0f), Quaternion.identity);
        newObject.transform.localScale = new Vector3(TileSize, TileSize, 1.0f);
        return newObject;
    }

    private void SpawnTile(GameObject tileType, int x, int y)
    {
        //If we are spawning a blank tile (deleting) just destroy and replace
        if (tileType == null)
        {
            if (GetTile(x, y))
            {
                DestroyTile(GetTile(x, y)); 
            }
            return;
        }

        //Otherwise do all necessary checks
        if (!ValidateTilePlacement(tileType, x, y)) return;

        GameObject tile = GetTile(x, y);
        if (tile != null)
        {
            //If the tile we are trying to place is already at the position
            //Or we are trying to place on a non-blank tile
            if (tile.name.Contains(tileType.name) || (!tile.name.Contains("Blank")))
                return;
            else if (Instance.GameState.Currency < PrefabCost) //If we can't afford the tile, return
                return;
            else //Destroy the tile if we can afford the new one
                DestroyTile(tile);
        }

        //If we can't afford the tile, return (shouldn't need to check, but just in case
        if (!Instance.GameState.TrySpend(PrefabCost)) return;

        //Spawn the tile and set variables properly
        if (_audioClip != null)
        {
            GameSystems.Instance.AudioManager.PlaySFX(_audioClip);
        }
        
        Tiles[(y * _MapWidth) + x] = Spawn(tileType, x, y);
        Tiles[(y * _MapWidth) + x].transform.parent = gameObject.transform;
        Tile tileComponent = Tiles[(y * _MapWidth) + x].GetComponent<Tile>();
        tileComponent.SetPosition(x, y);
        tileComponent.TileCost = PrefabCost;

        //Set variables based on tile type
        switch (tileComponent.Type)
        {
            //Properly get all neighbor tiles if path
            case (Tile.TileType.PATH):
                if (x == 0)
                {
                    PlayerTile = Tiles[(y * _MapWidth) + x];
                    GameObject playerSide = GameObject.Find("PlayerSide");
                    if (playerSide != null)
                    {
                        playerSide.transform.position = Tiles[(y * _MapWidth) + x].transform.position;
                    }
                }
                else if (x == _MapWidth - 1)
                {
                    EnemyTile = Tiles[(y * _MapWidth) + x];
                    GameObject spawner = GameObject.Find("Enemy Spawner");
                    if (spawner != null)
                    {
                        spawner.transform.position = Tiles[(y * _MapWidth) + x].transform.position;
                    }
                }
                Path pathComponent = tileComponent as Path;
                Vector2Int[] neighborPositions = new Vector2Int[4] { new(x, y + 1), new(x + 1, y), new(x, y - 1), new(x - 1, y) };
                for (int i = 0; i < 4; ++i)
                {
                    GameObject possibleNeighbor = GetTile(neighborPositions[i].x, neighborPositions[i].y);
                    if (possibleNeighbor)
                    {
                        if (possibleNeighbor.GetComponent<Tile>().Type == Tile.TileType.PATH)
                        {
                            pathComponent.Neighbors.Add(possibleNeighbor.GetComponent<Path>());
                            possibleNeighbor.GetComponent<Path>().Neighbors.Add(pathComponent);
                            possibleNeighbor.GetComponent<Path>().UpdateSprite();
                        }
                    }
                }
                pathComponent.UpdateSprite();
                if(PlayerTile != null && EnemyTile != null && isValidPath == false)
                {
                    if(GetPath(PlayerTile.transform.position, EnemyTile.transform.position).Count != 0)
                        isValidPath = true;
                }
                break;
        }
    }

    private void DestroyTile(GameObject tile)
    {
        //Refund tile cost
        Instance.GameState.AddCurrency(tile.GetComponent<Tile>().TileCost);

        if (tile == PlayerTile)
        {
            PlayerTile = null; 
            isValidPath = false;
            GameObject playerSide = GameObject.Find("PlayerSide");
            if (playerSide != null)
            {
                playerSide.transform.position = new Vector3(8.0f, 0.0f, 0.0f);
            }
        }
        if (tile == EnemyTile)
        {
            EnemyTile = null;
            isValidPath = false;
            GameObject spawner = GameObject.Find("Enemy Spawner");
            if (spawner != null)
            {
                spawner.transform.position = new Vector3(8.0f, 0.0f, 0.0f);
            }
        }
        if (tile.GetComponent<Path>() != null)
        {
            Path pathComponent = tile.GetComponent<Path>();
            for (int i = 0; i < pathComponent.Neighbors.Count; ++i)
            {
                GetTile(pathComponent.Neighbors[i].GridPosition).GetComponent<Path>().Neighbors.Remove(pathComponent);
                GetTile(pathComponent.Neighbors[i].GridPosition).GetComponent<Path>().UpdateSprite();
            }
            if (PlayerTile != null && EnemyTile != null && isValidPath)
            {
                if (GetPath(PlayerTile.transform.position, EnemyTile.transform.position).Count == 0)
                    isValidPath = false;
            }
        }
        Destroy(tile);
    }

    bool ValidateTilePlacement(GameObject tileType, int x, int y)
    {
        switch (tileType.GetComponent<Tile>().Type)
        {
            case (Tile.TileType.PATH):
                if(x == 0)
                {
                    if(PlayerTile) return false;
                }
                else if (x == _MapWidth - 1)
                {
                    if(EnemyTile) return false;
                }
                Vector2Int[] neighborPositions = new Vector2Int[4] { new(x, y + 1), new(x + 1, y), new(x, y - 1), new(x - 1, y) };
                int pathNeighbors = 0;
                for(int i = 0; i < 4; ++i)
                {
                    GameObject tile = GetTile(neighborPositions[i].x, neighborPositions[i].y);
                    if (tile)
                    {
                        if (tile.GetComponent<Tile>().Type == Tile.TileType.PATH)
                        {
                            if (tile.GetComponent<Path>().Neighbors.Count >= 2) return false;
                            ++pathNeighbors;
                        }
                    }
                }
                if (pathNeighbors > 2)
                    return false;
                break;
        }
        return true;
    }

}

public class Path : Tile
{
    [HideInInspector]
    public Path Parent = null;
    [HideInInspector]
    public List<Path> Neighbors = new List<Path>();
    [HideInInspector]
    public GameObject OccupyingEntity = null;

    public Path() : base(TileType.PATH)
    {
    }

    //Set the sprite to the proper index in the spritesheet based on the neighbors of the path tile
    public void UpdateSprite()
    {
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        string filePath = "Sprites/Path";
        Sprite[] sprites = Resources.LoadAll<Sprite>(filePath);

        if (Neighbors.Count == 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
        else if (Neighbors.Count == 1)
        {
            if (Neighbors[0].GridPosition.y == GridPosition.y + 1 || Neighbors[0].GridPosition.y == GridPosition.y - 1) //North or South
            {
                spriteRenderer.sprite = sprites[1];
            }
            if (Neighbors[0].GridPosition.x == GridPosition.x + 1 || Neighbors[0].GridPosition.x == GridPosition.x - 1) //East or West
            {
                spriteRenderer.sprite = sprites[0];
            }
        }
        else
        {

            int[] neighborBits = new int[4] { 0, 0, 0, 0 };
            for (int i = 0; i < Neighbors.Count; ++i)
            {
                if (Neighbors[i].GridPosition.y == GridPosition.y + 1) //North
                {
                    neighborBits[0] = 1;
                    continue;
                }
                if (Neighbors[i].GridPosition.x == GridPosition.x + 1) //East
                {
                    neighborBits[1] = 1;
                    continue;
                }
                if (Neighbors[i].GridPosition.y == GridPosition.y - 1) //South
                {
                    neighborBits[2] = 1;
                    continue;
                }
                if (Neighbors[i].GridPosition.x == GridPosition.x - 1) //West
                {
                    neighborBits[3] = 1;
                    continue;
                }
            }
            if (neighborBits[0] == 1) //Check all North options
            {
                if (neighborBits[1] == 1) //Other neighbor is East
                {
                    spriteRenderer.sprite = sprites[3];
                }
                else if (neighborBits[2] == 1) //Other neighbor is South
                {
                    spriteRenderer.sprite = sprites[1];
                }
                else if (neighborBits[3] == 1) //Other neighbor is West
                {
                    spriteRenderer.sprite = sprites[2];
                }
            }
            else if (neighborBits[1] == 1) //Check all East options
            {
                if (neighborBits[0] == 1) //Other neighbor is North
                {
                    spriteRenderer.sprite = sprites[3];
                }
                else if (neighborBits[2] == 1) //Other neighbor is South
                {
                    spriteRenderer.sprite = sprites[5];
                }
                else if (neighborBits[3] == 1) //Other neighbor is West
                {
                    spriteRenderer.sprite = sprites[0];
                }
            }
            else if (neighborBits[2] == 1) //Check all South options
            {
                if (neighborBits[1] == 1) //Other neighbor is East
                {
                    spriteRenderer.sprite = sprites[5];
                }
                else if (neighborBits[0] == 1) //Other neighbor is North
                {
                    spriteRenderer.sprite = sprites[1];
                }
                else if (neighborBits[3] == 1) //Other neighbor is West
                {
                    spriteRenderer.sprite = sprites[4];
                }
            }
            else if (neighborBits[3] == 1) //Check all West options
            {
                if (neighborBits[1] == 1) //Other neighbor is East
                {
                    spriteRenderer.sprite = sprites[0];
                }
                else if (neighborBits[2] == 1) //Other neighbor is South
                {
                    spriteRenderer.sprite = sprites[4];
                }
                else if (neighborBits[0] == 1) //Other neighbor is North
                {
                    spriteRenderer.sprite = sprites[2];
                }
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}

/// Code from an academic project November to December 2025 focused on Procedural Generation
/// My goal was to make the script very designer friendly and have
/// every value be adjustable to be able to create a variety of maps.

public class PCG : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Maximum height and width of tile map
    public int MaxMapSize = 101;
    //Size of floor and wall tiles in Unity units
    public float GridSize = 3.0f;
    public Vector2Int startOffset = new Vector2Int(0, 20);
    public int setupYLimit = 40;

    //////////////////////////////////////////////////////////////////////////

    //Tilemap array to make sure we don't put walls over floors--has no effect during gameplay
    private GameObject[] TileMap;
    //The 0,0 point of the tile map array, since the map goes from -MaxMapSize/2 to +MaxMapSize/2
    private int TileMapMidPoint;

    private Vector2Int[] directionAdder = new Vector2Int[4];
    private List<Vector2Int>[] branchingTiles = new List<Vector2Int>[4];

    //A random number generator for this run
    private System.Random RNG;

    //Dictionary of all PCG prefabs
    public Dictionary<string, GameObject> Prefabs;

    //A static reference to allow easy access from other scripts
    public static PCG PCGObject;


    public bool frameByFrame = true;
    private enum Biome { RIGHT, DOWN, LEFT };
    private Biome currentBiome = Biome.RIGHT;
    private string floorToSpawn = "floor";
    private Vector2Int currentPos;
    private bool rightDone = false;
    private Vector2Int lastPosRight;
    private bool downDone = false;
    private Vector2Int lastPosDown;
    private bool leftDone = false;
    private Vector2Int lastPosLeft;

    private int[] tries = { 0, 0, 0 };
    private bool finishedGenerating = false;

    public int tryLimit = 10;

    ////Direction Weights
    private int[][] directionWeights = new int[3][];
    ////Corridor Variables
    private int[] minCorridorLength = new int[3];
    private int[] maxCorridorLength = new int[3];
    private int[] corridorBranchChance = new int[3];
    //Ribcage Corrider variables
    private int[] ribcageCorridorChance = new int[3];
    private int[] maxRibcageLength = new int[3];
    private int[] maxRibcageWidth = new int[3];
    //Cave Corrider variables
    private int[] caveCorridorChance = new int[3];
    private int[] minCaveWidth = new int[3];
    private int[] maxCaveWidth = new int[3];

    ////Room variables
    private int[] roomChance = new int[3];
    private int[] minRoomSize = new int[3];
    private int[] maxRoomSize = new int[3];
    //Room Alteration variables
    private int[] cornerDiagnalCutChance = new int[3];
    private int[] cornerSquareCutChance = new int[3];
    private int[] pillarChance = new int[3];
    private int[] pillarEvenExtraChance = new int[3];
    private int[] roomExitChance = new int[3];

    ////Enemy variables
    private int[] minEnemiesPerCorridor = new int[3];
    private int[] maxEnemiesPerCorridor = new int[3];
    private int[] minEnemiesPerRoom = new int[3];
    private int[] maxEnemiesPerRoom = new int[3];
    private List<string>[] enemiesToSpawn = new List<string>[3];

    ////Pickup variables
    private int[] minPickupsPerCorridor = new int[3];
    private int[] maxPickupsPerCorridor = new int[3];
    private int[] minPickupsPerRoom = new int[3];
    private int[] maxPickupsPerRoom = new int[3];
    private int[] pickupDropChance = new int[3];
    private string[][] pickupsToSpawn = new string[3][];
    ////End variables (keys/doors/portal)
    private bool[] endSpawned = new bool[3];
    private string[] endItem = new string[3];

    public float generateDelay = 0.1f;
    private float timer = 0.0f;

    private List<int[][]> bossRooms = new();
    private List<int[][]> resolutions = new();

    void Start()
    {
        directionAdder[0] = new(0, 1);
        directionAdder[1] = new(1, 0);
        directionAdder[2] = new(0, -1);
        directionAdder[3] = new(-1, 0);

        SetBiomeVariables();

        SetBossRooms();

        SetResolutions();

        //Store a static reference to this generator
        PCGObject = this;

        //Load all the prefabs into the prefabs dictionary
        LoadPrefabs();

        //Create the tile map
        if (MaxMapSize % 2 == 0)
            ++MaxMapSize; //This needs to be an odd number
        TileMap = new GameObject[MaxMapSize * MaxMapSize];
        TileMapMidPoint = (MaxMapSize * MaxMapSize) / 2; //The 0,0 point

        //Get a new random generator
        //Could feed it a set seed if need the same level for debugging purposes
        if (TMObject.seed != 0)
            RNG = new System.Random(TMObject.seed);
        else
            RNG = new System.Random();

        InitializeGeneration();
        //Generate a level
        if (!frameByFrame)
            GenerateLevel();
        else
        {
            finishedGenerating = false;
        }
    }

    void SetBiomeVariables()
    {
        //Biome Right (Cave like) D1
        directionWeights[0] = new int[4];
        directionWeights[0][0] = 24;
        directionWeights[0][1] = 43;
        directionWeights[0][2] = 24;
        directionWeights[0][3] = 9;
        branchingTiles[0] = new();
        minCorridorLength[0] = 2;
        maxCorridorLength[0] = 9;
        corridorBranchChance[0] = 20;
        ribcageCorridorChance[0] = 0;
        maxRibcageLength[0] = 0;
        maxRibcageWidth[0] = 0;
        caveCorridorChance[0] = 95;
        minCaveWidth[0] = 3;
        maxCaveWidth[0] = 6;
        roomChance[0] = 50;
        minRoomSize[0] = 3;
        maxRoomSize[0] = 10;
        cornerDiagnalCutChance[0] = 90;
        cornerSquareCutChance[0] = 20;
        pillarChance[0] = 0;
        pillarEvenExtraChance[0] = 0;
        roomExitChance[0] = 80;
        minEnemiesPerCorridor[0] = 0;
        maxEnemiesPerCorridor[0] = 2;
        minEnemiesPerRoom[0] = 2;
        maxEnemiesPerRoom[0] = 6;
        enemiesToSpawn[0] = new(); //Multiples to make certain enemies more rare
        enemiesToSpawn[0].Add("enemy");
        enemiesToSpawn[0].Add("enemy");
        enemiesToSpawn[0].Add("fast");
        enemiesToSpawn[0].Add("fast");
        enemiesToSpawn[0].Add("fast");
        enemiesToSpawn[0].Add("spread");
        enemiesToSpawn[0].Add("spread");
        enemiesToSpawn[0].Add("spread");
        pickupDropChance[0] = 15;
        minPickupsPerCorridor[0] = 0;
        maxPickupsPerCorridor[0] = 2;
        minPickupsPerRoom[0] = 0;
        maxPickupsPerRoom[0] = 3;
        pickupsToSpawn[0] = new string[] { "heart", "heart", "speedboost", "speedboost", "rangeboost", "shotboost" };
        endSpawned[0] = false;
        endItem[0] = "silverkey";

        //Biome Down (Only rooms, lots of cuts and pillars) D3
        directionWeights[1] = new int[4];
        directionWeights[1][0] = 10;
        directionWeights[1][1] = 24;
        directionWeights[1][2] = 42;
        directionWeights[1][3] = 24;
        branchingTiles[1] = new();
        minCorridorLength[1] = 2;
        maxCorridorLength[1] = 6;
        corridorBranchChance[1] = 15;
        ribcageCorridorChance[1] = 0;
        maxRibcageLength[1] = 0;
        maxRibcageWidth[1] = 0;
        caveCorridorChance[1] = 0;
        minCaveWidth[1] = 0;
        maxCaveWidth[1] = 6;
        roomChance[1] = 70;
        minRoomSize[1] = 3;
        maxRoomSize[1] = 12;
        cornerDiagnalCutChance[1] = 30;
        cornerSquareCutChance[1] = 30;
        pillarChance[1] = 45;
        pillarEvenExtraChance[1] = 50;
        roomExitChance[1] = 100;
        minEnemiesPerCorridor[1] = 0;
        minEnemiesPerRoom[1] = 3;
        maxEnemiesPerCorridor[1] = 4;
        maxEnemiesPerRoom[1] = 7;
        enemiesToSpawn[1] = new(); //Multiples to make certain enemies more rare
        enemiesToSpawn[1].Add("ultra");
        enemiesToSpawn[1].Add("ultra");
        enemiesToSpawn[1].Add("ultra");
        enemiesToSpawn[1].Add("ultra");
        enemiesToSpawn[1].Add("ultraHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("fastHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("spreadHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        enemiesToSpawn[1].Add("tankHard");
        pickupDropChance[1] = 18;
        minPickupsPerCorridor[1] = 0;
        maxPickupsPerCorridor[1] = 2;
        minPickupsPerRoom[1] = 0;
        maxPickupsPerRoom[1] = 2;
        pickupsToSpawn[1] = new string[] { "speedboost", "shotboost", "healthboost", "rangeboost" };
        endSpawned[1] = false;
        endItem[1] = "portalboss";

        //Biome Left (Mostly corridors and ribcages) D2
        directionWeights[2] = new int[4];
        directionWeights[2][0] = 23;
        directionWeights[2][1] = 9;
        directionWeights[2][2] = 23;
        directionWeights[2][3] = 45;
        branchingTiles[2] = new();
        minCorridorLength[2] = 2;
        maxCorridorLength[2] = 15;
        corridorBranchChance[2] = 20;
        ribcageCorridorChance[2] = 40;
        maxRibcageLength[2] = 9;
        maxRibcageWidth[2] = 3;
        caveCorridorChance[2] = 0;
        minCaveWidth[2] = 0;
        maxCaveWidth[2] = 6;
        roomChance[2] = 15;
        minRoomSize[2] = 3;
        maxRoomSize[2] = 8;
        cornerDiagnalCutChance[2] = 10;
        cornerSquareCutChance[2] = 10;
        pillarChance[2] = 5;
        pillarEvenExtraChance[2] = 20;
        roomExitChance[2] = 60;
        minEnemiesPerCorridor[2] = 2;
        maxEnemiesPerCorridor[2] = 5;
        minEnemiesPerRoom[2] = 3;
        maxEnemiesPerRoom[2] = 8;
        enemiesToSpawn[2] = new(); //Multiples to make certain enemies more rare
        enemiesToSpawn[2].Add("enemy");
        enemiesToSpawn[2].Add("fast");
        enemiesToSpawn[2].Add("fast");
        enemiesToSpawn[2].Add("fast");
        enemiesToSpawn[2].Add("fast");
        enemiesToSpawn[2].Add("spread");
        enemiesToSpawn[2].Add("spread");
        enemiesToSpawn[2].Add("spread");
        enemiesToSpawn[2].Add("spread");
        enemiesToSpawn[2].Add("spread");
        enemiesToSpawn[2].Add("tank");
        enemiesToSpawn[2].Add("tank");
        enemiesToSpawn[2].Add("tank");
        enemiesToSpawn[2].Add("tank");
        enemiesToSpawn[2].Add("tank");
        pickupDropChance[2] = 15;
        minPickupsPerCorridor[2] = 0;
        maxPickupsPerCorridor[2] = 1;
        minPickupsPerRoom[2] = 0;
        maxPickupsPerRoom[2] = 3;
        pickupsToSpawn[2] = new string[] { "heart", "shotboost", "healthboost", "rangeboost" };
        endSpawned[2] = false;
        endItem[2] = "goldkey";
    }

    void InitializeGeneration()
    {
        //Clear any game objects first, just in case
        ClearLevel();

        //Create the camera
        SpawnCamera();

        //Spawn central room/area
        SpawnTile(startOffset.x, startOffset.y);//Center

        SpawnTile(startOffset.x + 1, startOffset.y);//Right
        SpawnTile(startOffset.x + 2, startOffset.y);//Right
        SpawnRotateRight("bronzedoor", startOffset.x + 2, startOffset.y);
        SpawnTile(startOffset.x + 3, startOffset.y);//Right
        SpawnTile(startOffset.x + 4, startOffset.y);//Right

        SpawnTile(startOffset.x, startOffset.y - 1);//Down
        //Spawn("portalboss", startOffset.x, startOffset.y - 1);//Down
        SpawnTile(startOffset.x, startOffset.y - 2);//Down
        Spawn("golddoor", startOffset.x, startOffset.y - 2);
        SpawnTile(startOffset.x, startOffset.y - 3);//Down
        SpawnTile(startOffset.x, startOffset.y - 4);//Down

        SpawnTile(startOffset.x - 1, startOffset.y);//Left
        SpawnTile(startOffset.x - 2, startOffset.y);//Left
        SpawnRotateLeft("silverdoor", startOffset.x - 2, startOffset.y);
        SpawnTile(startOffset.x - 3, startOffset.y);//Left
        SpawnTile(startOffset.x - 4, startOffset.y);//Left

        //Spawn dividing walls
        for (int i = 1; i <= MaxMapSize / 2; i++)
        {
            SpawnWall(startOffset.x + i, startOffset.y + i);
            SpawnWall(startOffset.x - i, startOffset.y + i);
            SpawnWall(startOffset.x + i, startOffset.y - i);
            SpawnWall(startOffset.x - i, startOffset.y - i);
        }

        lastPosRight = new(startOffset.x + 4, startOffset.y);
        lastPosDown = new(startOffset.x, startOffset.y - 4);
        lastPosLeft = new(startOffset.x - 4, startOffset.y);

        ////Spawn the Setup
        //The path up
        for (int i = 1; i < 4; ++i)
        {
            SpawnTile(startOffset.x, startOffset.y + i);
        }
        //The room
        for (int x = -2; x <= 2; ++x)
        {
            for (int y = 1; y < 6; ++y)
            {
                SpawnTile(startOffset.x + x, startOffset.y + 3 + y);
            }
        }
        //The initial enemies
        Spawn("enemy", startOffset.x - 2, startOffset.y + 6);
        Spawn("enemy", startOffset.x, startOffset.y + 6);
        Spawn("enemy", startOffset.x + 2, startOffset.y + 6);

        for (int i = 0; i < 2; ++i)
        {
            SpawnTile(startOffset.x, startOffset.y + 9 + i);
        }
        Spawn("bronzekey", startOffset.x, startOffset.y + 10);

    }

    //Update is called every frame
    void Update()
    {
        //ESC to quit
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit(); //Don't need a return from this...
        }

        //T to generate a test room
        if (Input.GetKey(KeyCode.F))
        {
            InitialCinematicPan();
            return;
        }

        //R to reset the level
        if (Input.GetKey(KeyCode.R))
        {
            ResetLevel();
            return;
        }

        timer -= Time.deltaTime;
        if (timer < 0 && frameByFrame && !finishedGenerating)
        {
            timer = generateDelay;
            GenerateLevelEachFrame();
        }
    }

    //Generate an actual level to play
    void GenerateLevel()
    {
        //Do Right
        if (!rightDone)
        {
            currentBiome = Biome.RIGHT;
            floorToSpawn = "floorRight";
            currentPos = lastPosRight;
            if (PCGLogic())
                rightDone = true;
            else
                lastPosRight = currentPos;
        }
        //Do Down
        if (!downDone)
        {
            currentBiome = Biome.DOWN;
            floorToSpawn = "floorDown";
            currentPos = lastPosDown;
            if (PCGLogic())
                downDone = true;
            else
                lastPosDown = currentPos;
        }
        //Do Left
        if (!leftDone)
        {
            currentBiome = Biome.LEFT;
            floorToSpawn = "floorLeft";
            currentPos = lastPosLeft;
            if (PCGLogic())
                leftDone = true;
            else
                lastPosLeft = currentPos;
        }
        if (!rightDone || !downDone || !leftDone) return;

        finishedGenerating = true;

        LevelGenerationFinished();
    }

    void GenerateLevelEachFrame()
    {
        //Do Right
        if (!rightDone)
        {
            currentBiome = Biome.RIGHT;
            floorToSpawn = "floorRight";
            currentPos = lastPosRight;
            if (PCGLogic())
                rightDone = true;
            else
                lastPosRight = currentPos;
        }
        //Do Down
        if (!downDone)
        {
            currentBiome = Biome.DOWN;
            floorToSpawn = "floorDown";
            currentPos = lastPosDown;
            if (PCGLogic())
                downDone = true;
            else
                lastPosDown = currentPos;
        }
        //Do Left
        if (!leftDone)
        {
            currentBiome = Biome.LEFT;
            floorToSpawn = "floorLeft";
            currentPos = lastPosLeft;
            if (PCGLogic())
                leftDone = true;
            else
                lastPosLeft = currentPos;
        }
        if (!rightDone || !downDone || !leftDone) return;

        finishedGenerating = true;

        LevelGenerationFinished();
    }

    void LevelGenerationFinished()
    {
        //Do an initial cinematic pan (if desired)
        InitialCinematicPan();

        //Fill all empty tiles with walls
        FillWithWalls();

        Spawn("player", startOffset.x, startOffset.y);
    }

    //Generate a test room with enemies, pick-ups, etc.
    void GenerateTestRoom()
    {
        //Clear any game object first
        ClearLevel();

        //Create the camera
        SpawnCamera();

        //Create the starting tile
        SpawnTile(0, 0);
        Spawn("player", 0.0f, 0.0f);

        //Create a square room
        //Note that already placed tiles will not be over-written
        for (int x = -5; x <= 5; x++)
            for (int y = -5; y <= 5; y++)
                SpawnTile(x, y);

        //Put a bunch of pick-ups around the player
        Spawn("heart", 0.5f, 0.5f);
        Spawn("healthboost", 0.0f, 0.5f);
        Spawn("speedboost", 0.5f, 0.0f);
        Spawn("shotboost", 0.5f, -0.5f);
        Spawn("heart", -0.5f, -0.5f);
        Spawn("healthboost", 0.0f, -0.5f);
        Spawn("speedboost", -0.5f, 0.0f);
        Spawn("shotboost", -0.5f, 0.5f);
        Spawn("heart", 1.5f, 1.5f);
        Spawn("healthboost", 1.0f, 1.5f);
        Spawn("speedboost", 1.5f, 1.0f);
        Spawn("shotboost", 1.5f, -1.5f);
        Spawn("heart", -1.5f, -1.5f);
        Spawn("healthboost", 1.0f, -1.5f);
        Spawn("speedboost", -1.5f, 1.0f);
        Spawn("shotboost", -1.5f, 1.5f);

        //Add some test enemies
        Spawn("enemy", 4.5f, 4.5f);
        Spawn("fast", 0.0f, 4.5f);
        Spawn("tank", 4.5f, 0.0f);
        Spawn("ultra", -4.5f, 4.5f);
        Spawn("spread", -4.5f, -4.5f);
        Spawn("boss", 0.0f, -4.5f);

        //Don't forget the exit...
        Spawn("portal", 5.0f, -5.0f);

        //Fill all empty tiles with walls
        FillWithWalls();
    }

    bool PCGLogic()
    {

        while (tries[(int)currentBiome] < tryLimit)
        {
            int direction = RNG.Next(100);
            if (direction < directionWeights[(int)currentBiome][0])
            {
                direction = 0;
            }
            else if (direction - directionWeights[(int)currentBiome][0] < directionWeights[(int)currentBiome][1])
            {
                direction = 1;
            }
            else if (direction - directionWeights[(int)currentBiome][0] - directionWeights[(int)currentBiome][1] < directionWeights[(int)currentBiome][2])
            {
                direction = 2;
            }
            else
            {
                direction = 3;
            }
            if (RNG.Next(100) < roomChance[(int)currentBiome])
            {
                if (GenerateRoom(direction) && frameByFrame)
                    return false;
            }
            else
            {
                if (RNG.Next(0, 100) < caveCorridorChance[(int)currentBiome])
                {
                    if (GenerateCaveCorridor(direction) && frameByFrame)
                        return false;
                }
                else
                {
                    if (GenerateCorridor(direction) && frameByFrame)
                        return false;

                }
            }
        }
        return true;
    }

    void GenerateFail()
    {
        tries[(int)currentBiome]++;
        if (tries[(int)currentBiome] >= 10)
        {
            if (branchingTiles[(int)currentBiome].Count > 0)
            {
                currentPos = branchingTiles[(int)currentBiome][0];
                branchingTiles[(int)currentBiome].RemoveAt(0);
                tries[(int)currentBiome] = 0;
            }
            else
            {
                if (!endSpawned[(int)currentBiome])
                {
                    Spawn(endItem[(int)currentBiome], currentPos.x, currentPos.y);
                    endSpawned[(int)currentBiome] = true;
                }
            }
        }
    }
    void SpawnEnemiesAndPickups(int left, int right, int top, int bottom, bool room = false)
    {
        int numEnemies, numPickups;
        if (room)
        {
            numEnemies = RNG.Next(minEnemiesPerRoom[(int)currentBiome], maxEnemiesPerRoom[(int)currentBiome] + 1);
            numPickups = RNG.Next(minPickupsPerRoom[(int)currentBiome], maxPickupsPerRoom[(int)currentBiome] + 1);
        }
        else
        {
            numEnemies = RNG.Next(minEnemiesPerCorridor[(int)currentBiome], maxEnemiesPerCorridor[(int)currentBiome] + 1);
            numPickups = RNG.Next(minPickupsPerCorridor[(int)currentBiome], maxPickupsPerCorridor[(int)currentBiome] + 1);
        }

        int spawnedEnemies = 0, spawnedPickups = 0;
        for (int x = left; x <= right; ++x)
        {
            for (int y = bottom; y <= top; ++y)
            {
                if ((GetTile(x, y) != null && !GetTile(x, y).name.Contains("Wall")))
                {
                    if (spawnedEnemies < numEnemies && RNG.Next(100) < 25)
                    {
                        spawnedEnemies++;
                        int enemyType = RNG.Next(0, enemiesToSpawn[(int)currentBiome].Count);
                        string enemy = enemiesToSpawn[(int)currentBiome][enemyType];
                        Spawn(enemy, x, y);
                    }
                    else if (spawnedPickups < numPickups && RNG.Next(100) < pickupDropChance[(int)currentBiome])
                    {
                        spawnedPickups++;
                        int pickupType = RNG.Next(0, pickupsToSpawn[(int)currentBiome].Length);
                        string pickup = pickupsToSpawn[(int)currentBiome][pickupType];
                        Spawn(pickup, x, y);
                    }
                }
                if (spawnedEnemies >= numEnemies && spawnedPickups >= numPickups) break;
            }
            if (spawnedEnemies >= numEnemies && spawnedPickups >= numPickups) break;
        }
    }
    bool GenerateCorridor(int direction)
    {
        int sizeTries = 3;
        while (sizeTries > 0)
        {
            Vector2Int corridorBegin = currentPos + directionAdder[direction];
            int corridorLength = RNG.Next(minCorridorLength[(int)currentBiome], maxCorridorLength[(int)currentBiome] + 1);
            Vector2Int corridorEnd = corridorBegin + (directionAdder[direction] * corridorLength);
            int xBuf = 1, yBuf = 1;
            if (direction % 2 == 0)
            {
                yBuf = 0;
            }
            else
            {
                xBuf = 0;
            }
            int left = corridorBegin.x, right = corridorEnd.x, bottom = corridorBegin.y, top = corridorEnd.y;
            if (corridorBegin.x > corridorEnd.x)
            {
                left = corridorEnd.x;
                right = corridorBegin.x;
            }
            if (corridorBegin.y > corridorEnd.y)
            {
                bottom = corridorEnd.y;
                top = corridorBegin.y;
            }
            if (CheckRoom(left, right, bottom, top, xBuf, yBuf))
            {
                tries[(int)currentBiome] = 0;
                SpawnRoom(left, right, bottom, top);
                //RibcageCorridor
                if (RNG.Next(100) < ribcageCorridorChance[(int)currentBiome])
                {
                    int length = RNG.Next(1, maxRibcageLength[(int)currentBiome] + 1);
                    int width = RNG.Next(1, maxRibcageWidth[(int)currentBiome] + 1);
                    int alternate = RNG.Next(0, 2);
                    bool flipper = true;
                    if (direction == 0 || direction == 2)
                    {
                        for (int i = bottom + 1; i <= top; i += (width + 1))
                        {
                            if (flipper)
                            {
                                if (CheckRoom(left - length - 1, right - 1, i, i + width - 1, 1, 1, width + 2))
                                {
                                    SpawnRoom(left - length, right - 1, i, i + width - 1);
                                    if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(new(left - length, RNG.Next(i, i + width - 1)));
                                    }
                                }
                                if (alternate == 0)
                                {
                                    if (CheckRoom(left + 1, right + length + 1, i, i + width - 1, 1, 1, width + 2))
                                    {
                                        SpawnRoom(left + 1, right + length, i, i + width - 1);
                                        if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                        {
                                            branchingTiles[(int)currentBiome].Add(new(right + length, RNG.Next(i, i + width - 1)));
                                        }
                                    }
                                }
                                flipper = false;
                            }
                            else
                            {
                                if (CheckRoom(left + 1, right + length + 1, i, i + width - 1, 1, 1, width + 2))
                                {
                                    SpawnRoom(left + 1, right + length, i, i + width - 1);
                                    if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(new(right + length, RNG.Next(i, i + width - 1)));
                                    }
                                }
                                if (alternate == 0)
                                {
                                    if (CheckRoom(left - length - 1, right - 1, i, i + width - 1, 1, 1, width + 2))
                                    {
                                        SpawnRoom(left - length, right - 1, i, i + width - 1);
                                        if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                        {
                                            branchingTiles[(int)currentBiome].Add(new(left - length, RNG.Next(i, i + width - 1)));
                                        }
                                    }
                                }
                                flipper = true;
                            }
                        }
                        //SpawnEnemiesAndPickups
                        SpawnEnemiesAndPickups(left - length - 1, right + length + 1, top, bottom);
                    }
                    else
                    {
                        for (int i = left + 1; i <= right; i += (width + 1))
                        {
                            if (flipper)
                            {
                                if (CheckRoom(i, i + width - 1, bottom + 1, top + length + 1, 1, 1, width + 2))
                                {
                                    SpawnRoom(i, i + width - 1, bottom + 1, top + length);
                                    if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(new(RNG.Next(i, i + width - 1), top + length));
                                    }
                                }
                                if (alternate == 0)
                                {
                                    if (CheckRoom(i, i + width - 1, bottom - length - 1, top - 1, 1, 1, width + 2))
                                    {
                                        SpawnRoom(i, i + width - 1, bottom - length, top - 1);
                                        if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                        {
                                            branchingTiles[(int)currentBiome].Add(new(RNG.Next(i, i + width - 1), bottom - length));
                                        }
                                    }
                                }
                                flipper = false;
                            }
                            else
                            {
                                if (CheckRoom(i, i + width - 1, bottom - length - 1, top - 1, 1, 0, width))
                                {
                                    SpawnRoom(i, i + width - 1, bottom - length, top - 1);
                                    if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(new(RNG.Next(i, i + width - 1), bottom - length));
                                    }
                                }
                                if (alternate == 0)
                                {
                                    if (CheckRoom(i, i + width - 1, bottom + 1, top + length + 1, 1, 0, width))
                                    {
                                        SpawnRoom(i, i + width - 1, bottom + 1, top + length);
                                        if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                                        {
                                            branchingTiles[(int)currentBiome].Add(new(RNG.Next(i, i + width - 1), top + length));
                                        }
                                    }
                                }
                                flipper = true;
                            }
                        }
                        //SpawnEnemiesAndPickups
                        SpawnEnemiesAndPickups(left, right, top + length + 1, bottom - length - 1);
                    }
                }
                else
                {
                    for (int i = left; i <= right; ++i)
                    {
                        for (int j = bottom; j <= top; ++j)
                        {
                            if (RNG.Next(100) < corridorBranchChance[(int)currentBiome])
                            {
                                branchingTiles[(int)currentBiome].Add(new(i, j));
                            }
                        }
                    }
                    //Spawn Enemies
                    SpawnEnemiesAndPickups(left, right, top, bottom);
                }
                currentPos = corridorEnd;
                return true;
            }
            sizeTries--;
        }
        GenerateFail();
        return false;
    }

    bool GenerateCaveCorridor(int direction)
    {
        int sizeTries = 3;
        while (sizeTries > 0)
        {
            int corridorLength = RNG.Next(minCorridorLength[(int)currentBiome], maxCorridorLength[(int)currentBiome] + 1);
            int caveWidth = RNG.Next(1, maxCaveWidth[(int)currentBiome]);
            Vector2Int corridorBegin = currentPos + directionAdder[direction];
            Vector2Int corridorEnd = corridorBegin + (directionAdder[direction] * corridorLength);
            int xBuf = 1, yBuf = 1;
            int left = corridorBegin.x, right = corridorEnd.x, bottom = corridorBegin.y, top = corridorEnd.y;
            if (corridorBegin.x > corridorEnd.x)
            {
                left = corridorEnd.x;
                right = corridorBegin.x;
            }
            if (corridorBegin.y > corridorEnd.y)
            {
                bottom = corridorEnd.y;
                top = corridorBegin.y;
            }

            if (direction % 2 == 0)
            {
                yBuf = 0;
                left -= caveWidth / 2;
                right += caveWidth / 2;
            }
            else
            {
                xBuf = 0;
                bottom -= caveWidth / 2;
                top += caveWidth / 2;
            }
            if (CheckRoom(left, right, bottom, top, xBuf, yBuf))
            {
                tries[(int)currentBiome] = 0;
                switch (direction)
                {
                    case 0:
                        {
                            Vector2Int caveEntrance = corridorBegin;
                            SpawnTile(caveEntrance.x, caveEntrance.y);
                            while (caveEntrance.y < top)
                            {
                                int caveDirection = RNG.Next(0, 4);
                                if (caveDirection == 2) continue;
                                Vector2Int newTile = caveEntrance + directionAdder[caveDirection];
                                if (GetTile(newTile.x, newTile.y) == null
                                    && newTile.x >= left
                                    && newTile.x <= right)
                                {
                                    SpawnTile(newTile.x, newTile.y);
                                    if (RNG.Next(0, 100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(newTile);
                                    }
                                    caveEntrance = newTile;
                                }
                            }
                            corridorEnd = caveEntrance;
                            break;
                        }
                    case 1:
                        {
                            Vector2Int caveEntrance = corridorBegin;
                            SpawnTile(caveEntrance.x, caveEntrance.y);
                            while (caveEntrance.x < right)
                            {
                                int caveDirection = RNG.Next(0, 4);
                                if (caveDirection == 3) continue;
                                Vector2Int newTile = caveEntrance + directionAdder[caveDirection];
                                if (GetTile(newTile.x, newTile.y) == null
                                    && newTile.y >= bottom
                                    && newTile.y <= top)
                                {
                                    SpawnTile(newTile.x, newTile.y);
                                    if (RNG.Next(0, 100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(newTile);
                                    }
                                    caveEntrance = newTile;
                                }
                            }
                            corridorEnd = caveEntrance;
                            break;
                        }
                    case 2:
                        {
                            Vector2Int caveEntrance = corridorBegin;
                            SpawnTile(caveEntrance.x, caveEntrance.y);
                            while (caveEntrance.y > bottom)
                            {
                                int caveDirection = RNG.Next(0, 4);
                                if (caveDirection == 0) continue;
                                Vector2Int newTile = caveEntrance + directionAdder[caveDirection];
                                if (GetTile(newTile.x, newTile.y) == null
                                    && newTile.x >= left
                                    && newTile.x <= right)
                                {
                                    SpawnTile(newTile.x, newTile.y);
                                    if (RNG.Next(0, 100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(newTile);
                                    }
                                    caveEntrance = newTile;
                                }
                            }
                            corridorEnd = caveEntrance;
                            break;
                        }
                    case 3:
                        {
                            Vector2Int caveEntrance = corridorBegin;
                            SpawnTile(caveEntrance.x, caveEntrance.y);
                            while (caveEntrance.x > left)
                            {
                                int caveDirection = RNG.Next(0, 4);
                                if (caveDirection == 1) continue;
                                Vector2Int newTile = caveEntrance + directionAdder[caveDirection];
                                if (GetTile(newTile.x, newTile.y) == null
                                    && newTile.y >= bottom
                                    && newTile.y <= top)
                                {
                                    SpawnTile(newTile.x, newTile.y);
                                    if (RNG.Next(0, 100) < corridorBranchChance[(int)currentBiome])
                                    {
                                        branchingTiles[(int)currentBiome].Add(newTile);
                                    }
                                    caveEntrance = newTile;
                                }
                            }
                            corridorEnd = caveEntrance;
                            break;
                        }
                }
                currentPos = corridorEnd;
                //Spawn Enemies
                SpawnEnemiesAndPickups(left, right, top, bottom);
                return true;
            }
            --sizeTries;
        }
        GenerateFail();
        return false;
    }

    bool GenerateRoom(int direction)
    {
        Vector2Int entrance = currentPos + directionAdder[direction];
        if (GetTile(entrance.x, entrance.y) != null) return false;
        int sizeTries = 5;
        while (sizeTries > 0)
        {
            int roomWidth = RNG.Next(minRoomSize[(int)currentBiome], maxRoomSize[(int)currentBiome]);
            if (roomWidth % 2 != 0) ++roomWidth;
            int roomHeight = RNG.Next(minRoomSize[(int)currentBiome], maxRoomSize[(int)currentBiome]);
            if (roomHeight % 2 != 0) ++roomHeight;
            int left = entrance.x, right = entrance.x, bottom = entrance.y, top = entrance.y;
            switch (direction)
            {
                case 0:
                    left = entrance.x - (roomWidth / 2);
                    right = entrance.x + (roomWidth / 2);
                    bottom = entrance.y;
                    top = entrance.y + roomHeight;
                    break;
                case 1:
                    left = entrance.x;
                    right = entrance.x + roomWidth;
                    bottom = entrance.y - (roomHeight / 2);
                    top = entrance.y + (roomHeight / 2);
                    break;
                case 2:
                    left = entrance.x - (roomWidth / 2);
                    right = entrance.x + (roomWidth / 2);
                    bottom = entrance.y - roomHeight;
                    top = entrance.y;
                    break;
                case 3:
                    left = entrance.x - roomWidth;
                    right = entrance.x;
                    bottom = entrance.y - (roomHeight / 2);
                    top = entrance.y + (roomHeight / 2);
                    break;
            }
            if (CheckRoom(left, right, bottom, top, 1, 1, 1))
            {
                tries[(int)currentBiome] = 0;
                SpawnTile(entrance.x, entrance.y);
                SpawnRoom(left, right, bottom, top);

                //Cutting corners
                for (int corner = 0; corner < 4; ++corner)
                {
                    if (RNG.Next(100) < cornerDiagnalCutChance[(int)currentBiome])
                    {
                        int max = Math.Min(roomWidth, roomHeight) / 2;
                        int cutSize = RNG.Next(2, max);
                        switch (corner)
                        {
                            case 0: //Top Left Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize - i; ++j)
                                    {
                                        DeleteTile(left + j, top - i);
                                    }
                                }
                                break;
                            case 1: //Top Right Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize - i; ++j)
                                    {
                                        DeleteTile(right - j, top - i);
                                    }
                                }
                                break;
                            case 2: //Bottom Left Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize - i; ++j)
                                    {
                                        DeleteTile(left + j, bottom + i);
                                    }
                                }
                                break;
                            case 3: //Bottom Right Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize - i; ++j)
                                    {
                                        DeleteTile(right - j, bottom + i);
                                    }
                                }
                                break;
                        }
                    }
                    else if (RNG.Next(100) < cornerSquareCutChance[(int)currentBiome])
                    {
                        int max = Math.Min(roomWidth, roomHeight) / 2;
                        int cutSize = RNG.Next(2, max);
                        switch (corner)
                        {
                            case 0: //Top Left Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize; ++j)
                                    {
                                        DeleteTile(left + j, top - i);
                                    }
                                }
                                break;
                            case 1: //Top Right Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize; ++j)
                                    {
                                        DeleteTile(right - j, top - i);
                                    }
                                }
                                break;
                            case 2: //Bottom Left Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize; ++j)
                                    {
                                        DeleteTile(left + j, bottom + i);
                                    }
                                }
                                break;
                            case 3: //Bottom Right Corner
                                for (int i = 0; i < cutSize; ++i)
                                {
                                    for (int j = 0; j < cutSize; ++j)
                                    {
                                        DeleteTile(right - j, bottom + i);
                                    }
                                }
                                break;
                        }
                    }
                }

                //Adding pillars
                if (RNG.Next(100) < pillarChance[(int)currentBiome])
                {

                    int pillarWidth = RNG.Next(1, Mathf.Min(roomWidth, roomHeight) / 3);
                    int pillarHeight = RNG.Next(1, Mathf.Min(roomWidth, roomHeight) / 3);
                    int distanceFromEdges = RNG.Next(1, Mathf.Min(roomWidth, roomHeight) / 3);
                    for (int pH = 0; pH < pillarWidth; ++pH)
                    {
                        for (int pV = 0; pV < pillarHeight; ++pV)
                        {
                            DeleteTile(left + distanceFromEdges + pH, top - distanceFromEdges - pV);
                            DeleteTile(right - distanceFromEdges - pH, top - distanceFromEdges - pV);
                            DeleteTile(left + distanceFromEdges + pH, bottom + distanceFromEdges + pV);
                            DeleteTile(right - distanceFromEdges - pH, bottom + distanceFromEdges + pV);
                        }
                    }
                    if (RNG.Next(100) < pillarEvenExtraChance[(int)currentBiome])
                    {
                        int newPillarSizeEven = RNG.Next(1, Mathf.Min(roomWidth, roomHeight) / 3);
                        int newPillarSizeWhatever = RNG.Next(1, Mathf.Min(roomWidth, roomHeight) / 3);
                        if (newPillarSizeEven % 2 != 0) ++newPillarSizeEven;
                        int newDistanceFromEdge = RNG.Next(1, Mathf.Min(roomWidth, roomHeight) / 3);
                        if (roomWidth > roomHeight)
                        {
                            for (int pH = -(newPillarSizeEven / 2); pH <= (newPillarSizeEven / 2); ++pH)
                            {
                                for (int pV = 0; pV < newPillarSizeWhatever; ++pV)
                                {
                                    DeleteTile(left + (roomWidth / 2) + pH, top - newDistanceFromEdge - pV);
                                    DeleteTile(left + (roomWidth / 2) + pH, bottom + newDistanceFromEdge + pV);
                                }
                            }
                        }
                        else
                        {
                            for (int pV = -(newPillarSizeEven / 2); pV <= (newPillarSizeEven / 2); ++pV)
                            {
                                for (int pH = 0; pH < newPillarSizeWhatever; ++pH)
                                {
                                    DeleteTile(left + newDistanceFromEdge + pH, bottom + (roomHeight / 2) + pV);
                                    DeleteTile(right - newDistanceFromEdge - pH, bottom + (roomHeight / 2) + pV);
                                }
                            }
                        }
                    }
                }
                //Adding random exits to other directions
                List<Vector2Int> exits = new();
                for (int i = 0; i < 4; ++i)
                {
                    if (i == ((direction + 2) % 4)) continue;
                    if (RNG.Next(100) < roomExitChance[(int)currentBiome])
                    {
                        List<Vector2Int> possibleExits = new();
                        int randomTile;
                        switch (i)
                        {
                            case 0:
                                for (int alongTop = left; alongTop <= right; ++alongTop)
                                {
                                    if (GetTile(alongTop, top) != null)
                                    {
                                        possibleExits.Add(new(alongTop, top));
                                    }
                                }
                                randomTile = RNG.Next(0, possibleExits.Count);
                                branchingTiles[(int)currentBiome].Add(possibleExits[randomTile]);
                                exits.Add(possibleExits[randomTile]);
                                break;
                            case 1:
                                for (int alongRight = bottom; alongRight <= top; ++alongRight)
                                {
                                    if (GetTile(right, alongRight) != null)
                                    {
                                        possibleExits.Add(new(right, alongRight));
                                    }
                                }
                                randomTile = RNG.Next(0, possibleExits.Count);
                                branchingTiles[(int)currentBiome].Add(possibleExits[randomTile]);
                                exits.Add(possibleExits[randomTile]);
                                break;
                            case 2:
                                for (int alongBottom = left; alongBottom <= right; ++alongBottom)
                                {
                                    if (GetTile(alongBottom, bottom) != null)
                                    {
                                        possibleExits.Add(new(alongBottom, bottom));
                                    }
                                }
                                randomTile = RNG.Next(0, possibleExits.Count);
                                branchingTiles[(int)currentBiome].Add(possibleExits[randomTile]);
                                exits.Add(possibleExits[randomTile]);
                                break;
                            case 3:
                                for (int alongLeft = bottom; alongLeft <= top; ++alongLeft)
                                {
                                    if (GetTile(left, alongLeft) != null)
                                    {
                                        possibleExits.Add(new(left, alongLeft));
                                    }
                                }
                                randomTile = RNG.Next(0, possibleExits.Count);
                                branchingTiles[(int)currentBiome].Add(possibleExits[randomTile]);
                                exits.Add(possibleExits[randomTile]);
                                break;
                        }
                    }
                }
                //Pick a random exit to continue generation from
                if (exits.Count > 0)
                {
                    int newCurrent = RNG.Next(0, exits.Count);
                    currentPos = exits[newCurrent];
                    branchingTiles[(int)currentBiome].Remove(currentPos);
                }
                else if (branchingTiles[(int)currentBiome].Count > 0)
                {
                    //Get a new tile of the branch list if no exits were added
                    int newCurrent = RNG.Next(0, branchingTiles[(int)currentBiome].Count);
                    currentPos = branchingTiles[(int)currentBiome][newCurrent];
                    branchingTiles[(int)currentBiome].RemoveAt(newCurrent);
                }
                else
                {
                    //We are out of tiles, make sure we spawned the end item
                    if (!endSpawned[(int)currentBiome])
                    {
                        //Try to spawn from the center of the room
                        int x = left + (roomWidth / 2), y = bottom + (roomHeight / 2);
                        for (int dis = 0; dis < Math.Min(roomHeight, roomWidth) && !endSpawned[(int)currentBiome]; ++dis)
                        {
                            if (dis == 0)
                            {
                                if (GetTile(x, y))
                                {
                                    Spawn(endItem[(int)currentBiome], x, y);
                                    endSpawned[(int)currentBiome] = true;
                                }
                            }
                            else
                            {
                                if (GetTile(x, y + dis)) //North
                                {
                                    Spawn(endItem[(int)currentBiome], x, y + dis);
                                    endSpawned[(int)currentBiome] = true;
                                }
                                else if (GetTile(x + dis, y + dis)) //NorthEast
                                {
                                    Spawn(endItem[(int)currentBiome], x + dis, y + dis);
                                    endSpawned[(int)currentBiome] = true;
                                }
                                else if (GetTile(x + dis, y)) //East
                                {
                                    Spawn(endItem[(int)currentBiome], x + dis, y);
                                    endSpawned[(int)currentBiome] = true;
                                }
                                else if (GetTile(x + dis, y - dis)) //SouthEast
                                {
                                    Spawn(endItem[(int)currentBiome], x + dis, y - dis);
                                    endSpawned[(int)currentBiome] = true;
                                }
                                else if (GetTile(x, y - dis)) //South
                                {
                                    Spawn(endItem[(int)currentBiome], x + dis, y + dis);
                                    endSpawned[(int)currentBiome] = true;
                                }
                                else if (GetTile(x - dis, y - dis)) //SouthWest
                                {
                                    Spawn(endItem[(int)currentBiome], x - dis, y - dis);
                                    endSpawned[(int)currentBiome] = true;
                                }
                                else if (GetTile(x - dis, y)) //West
                                {
                                    Spawn(endItem[(int)currentBiome], x - dis, y);
                                    endSpawned[(int)currentBiome] = true;
                                }
                                else if (GetTile(x - dis, y + dis)) //NorthWest
                                {
                                    Spawn(endItem[(int)currentBiome], x - dis, y + dis);
                                    endSpawned[(int)currentBiome] = true;
                                }
                            }
                        }
                    }
                }
                //Spawn Enemies
                SpawnEnemiesAndPickups(left, right, top, bottom, true);
                return true;
            }
            --sizeTries;
        }
        GenerateFail();
        return false;
    }

    void SetBossRooms()
    {
        //Corner cut room
        int[][] bossRoom = new int[21][] {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 0, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 0, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 }
        };

        bossRooms.Add(bossRoom);

        bossRoom = new int[21][] {
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 3, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 3, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1 },
            new int[] { 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1 },
            new int[] { 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 3, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 3, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        bossRooms.Add(bossRoom);

        bossRoom = new int[21][] {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1 },
            new int[] { 1, 1, 1, 0, 0, 3, 1, 1, 1, 1, 2, 1, 1, 1, 1, 3, 0, 0, 1, 1, 1 },
            new int[] { 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 }
        };

        bossRooms.Add(bossRoom);
    }

    void SetResolutions()
    {
        //Corner cut room
        int[][] resolution = new int[11][] {
            new int[] { 0, 0, 6, 0, 0 },
            new int[] { 10, 9, 1, 9, 10 },
            new int[] { 1, 11, 1, 11, 1 },
            new int[] { 1, 1, 1, 1, 1 },
            new int[] { 0, 0, 1, 0, 0 },
            new int[] { 0, 0, 4, 0, 0 },
            new int[] { 0, 0, 5, 0, 0 },
            new int[] { 0, 0, 4, 0, 0 },
            new int[] { 0, 0, 5, 0, 0 },
            new int[] { 0, 0, 3, 0, 0 },
            new int[] { 0, 0, 7, 0, 0 }
        };

        resolutions.Add(resolution);

        resolution = new int[11][] {
            new int[] { 0, 0, 6, 0, 0 },
            new int[] { 0, 1, 13, 1, 0 },
            new int[] { 0, 13, 12, 13, 0 },
            new int[] { 0, 1, 1, 1, 0 },
            new int[] { 0, 0, 1, 0, 0 },
            new int[] { 0, 0, 4, 0, 0 },
            new int[] { 0, 0, 5, 0, 0 },
            new int[] { 0, 0, 4, 0, 0 },
            new int[] { 0, 0, 5, 0, 0 },
            new int[] { 0, 0, 3, 0, 0 },
            new int[] { 0, 0, 7, 0, 0 }
        };

        resolutions.Add(resolution);

        resolution = new int[11][] {
            new int[] { 0, 0, 6, 0, 0 },
            new int[] { 0, 0, 1, 0, 0 },
            new int[] { 0, 0, 1, 0, 0 },
            new int[] { 0, 12, 12, 12, 0 },
            new int[] { 0, 14, 13, 14, 0 },
            new int[] { 0, 14, 13, 14, 0 },
            new int[] { 0, 1, 13, 1, 0 },
            new int[] { 0, 1, 1, 1, 0 },
            new int[] { 0, 0, 4, 0, 0 },
            new int[] { 0, 0, 3, 0, 0 },
            new int[] { 0, 0, 7, 0, 0 }
        };

        resolutions.Add(resolution);
    }
    public IEnumerator StartBossEncounter(int Speed, int Health, int Shots, float Range)
    {
        yield return new WaitForSeconds(0.1f);

        Dictionary<int, string> bossTiles = new Dictionary<int, string>();

        bossTiles[2] = "boss";
        bossTiles[3] = "healthboost";
        bossTiles[4] = "shotboost";
        bossTiles[5] = "rangeboost";
        bossTiles[6] = "portalend";
        bossTiles[7] = "bossdoor";
        bossTiles[8] = "ultra";
        bossTiles[9] = "tankHard";
        bossTiles[10] = "spreadHard";
        bossTiles[11] = "fastHard";
        bossTiles[12] = "tank";
        bossTiles[13] = "spread";
        bossTiles[14] = "fast";

        ClearLevel();

        //Create the camera
        SpawnCamera();

        floorToSpawn = "floor";

        //Spawn the boss area
        for (int i = 0; i < 10; ++i)
        {
            SpawnTile(0, -20 + i);
        }
        int bossRoomToSpawn = RNG.Next(bossRooms.Count);
        for (int y = 0; y < 21; ++y)
        {
            for (int x = 0; x < 21; ++x)
            {
                int tileValue = bossRooms[bossRoomToSpawn][y][x];
                if (tileValue > 0)
                {
                    SpawnTile(-10 + x, -10 + y);
                    if (tileValue > 1)
                        Spawn(bossTiles[tileValue], -10 + x, 10 - y);
                }
            }
        }
        //Spawn the resolution
        int resolutionToSpawn = RNG.Next(resolutions.Count);
        for (int y = 0; y < 11; ++y)
        {
            for (int x = 0; x < 5; ++x)
            {
                int tileValue = resolutions[resolutionToSpawn][y][x];
                if (tileValue > 0)
                {
                    SpawnTile(-2 + x, 21 - y);
                    if (tileValue > 1)
                        Spawn(bossTiles[tileValue], -2 + x, 21 - y);
                }
            }
        }


        //Write over the old player information so I don't have to deal with moving the player to the right spot
        GameObject newPlayer = Spawn("player", 0, -20.0f + 0.5f);
        PlayerLogic player = newPlayer.GetComponent<PlayerLogic>();
        player.StartingSpeed = Speed;
        player.StartingHealth = Health;
        player.StartingShots = Shots;
        player.StartingRange = Range;


        //Fill all empty tiles with walls
        FillWithWalls();

        //Do an initial cinematic pan (if desired)
        InitialCinematicPan();
    }

    //Clear the entire level except for the PCG object, Telemetry manager, and player
    void ClearLevel()
    {
        //Delete everything       
        var objsToDelete = FindObjectsOfType<GameObject>();
        for (int i = 0; i < objsToDelete.Length; i++)
        {
            if (objsToDelete[i].GetComponent<PCG>() == null && objsToDelete[i].GetComponent<TelemetryManager>() == null)
                UnityEngine.Object.DestroyImmediate(objsToDelete[i]);
        }
    }

    //Reset the level after a delay
    //This is public so it can be called from other files using the static PCGObject from above
    public void ResetLevel(float delay)
    {
        Invoke("ResetLevel", delay);
    }

    //Reset the level immediately
    //This is public so it can be called from other files using the static PCGObject from above
    public void ResetLevel()
    {
        var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    //Get a tile object (only walls and floors, currently)
    GameObject GetTile(int x, int y)
    {
        if (Math.Abs(x) > MaxMapSize / 2 || Math.Abs(y) > MaxMapSize / 2)
            return Prefabs["wall"];
        return TileMap[(y * MaxMapSize) + x + TileMapMidPoint];
    }

    //Spawn a tile object if somthing isn't already there
    void SpawnTile(int x, int y)
    {
        if (GetTile(x, y) != null)
            return;
        TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn(floorToSpawn, x, y);
    }

    void DeleteTile(int x, int y)
    {
        if (GetTile(x, y) == null) return;
        Destroy(TileMap[(y * MaxMapSize) + x + TileMapMidPoint]);
        TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = null;
    }

    bool CheckRoom(int left, int right, int bottom, int top, int xBufferSize = 1, int yBufferSize = 1, int allowedOverlaps = 0)
    {
        int overlap = 0;
        for (int i = (left - xBufferSize); i <= (right + xBufferSize); ++i)
        {
            for (int j = (bottom - yBufferSize); j <= (top + yBufferSize); ++j)
            {
                if (GetTile(i, j) != null)
                    ++overlap;
            }
        }
        if (overlap <= allowedOverlaps)
            return true;
        return false;
    }

    void SpawnRoom(int left, int right, int bottom, int top)
    {
        for (int i = left; i <= right; ++i)
        {
            for (int j = bottom; j <= top; ++j)
            {
                SpawnTile(i, j);
            }
        }
    }

    //Spawn a wall object if something isn't already there
    void SpawnWall(int x, int y)
    {
        if (GetTile(x, y) != null)
            return;
        TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn("wall", x, y);
    }

    //Fill any empty tiles with walls
    void FillWithWalls()
    {
        for (int x = -MaxMapSize / 2; x <= MaxMapSize / 2; x++)
            for (int y = -MaxMapSize / 2; y <= MaxMapSize / 2; y++)
                SpawnWall(x, y);

        //Spawn walls along the edges of the map, technically outside so we don't lose any map space
        for (int x = -(MaxMapSize / 2) - 1; x <= (MaxMapSize / 2) + 1; x++)
        {
            Spawn("wall", x, -(MaxMapSize / 2) - 1);
            Spawn("wall", x, (MaxMapSize / 2) + 1);
        }
        for (int y = -(MaxMapSize / 2); y <= (MaxMapSize / 2); y++)
        {
            Spawn("wall", -(MaxMapSize / 2) - 1, y);
            Spawn("wall", (MaxMapSize / 2) + 1, y);
        }
    }

    //Spawn any object
    GameObject Spawn(string obj, float x, float y)
    {
        return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.identity);
    }

    //Spawn any object rotated 90 degrees left
    GameObject SpawnRotateLeft(string obj, float x, float y)
    {
        return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.AngleAxis(-90, Vector3.forward));
    }

    //Spawn any object rotated 90 degrees right
    GameObject SpawnRotateRight(string obj, float x, float y)
    {
        return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.AngleAxis(90, Vector3.forward));
    }

    //Spawn main camera and the weighted camera target
    void SpawnCamera()
    {
        var cam = Instantiate(PCGObject.Prefabs["camera"]);
        var wct = Instantiate(PCGObject.Prefabs["cameratarget"]);
        cam.GetComponent<CameraFollow>().ObjectToFollow = wct.transform;
    }

    //Try to pan to something of interest
    void InitialCinematicPan()
    {
        //Pan to the first bronze key if one exists
        var panTo = GameObject.Find("BronzeKey(Clone)");
        //Otherwise pan to the first silver key if one exists
        if (panTo == null)
            panTo = GameObject.Find("SilverKey(Clone)");
        //Otherwise pan to the first gold key if one exists
        if (panTo == null)
            panTo = GameObject.Find("GoldKey(Clone)");
        //Otherwise pan to the first boss enemy if one exists
        if (panTo == null)
            panTo = GameObject.Find("BossEnemy(Clone)");
        //Otherwise pan to the first exit portal (which should exist)
        if (panTo == null)
            panTo = GameObject.Find("PortalBoss(Clone)");
        //Did we find something?
        if (panTo == null)
            return;
        //Create a cinematic anchor to pan to, and set it's position
        var cinematicAnchor = Instantiate(PCGObject.Prefabs["cinematicanchor"]);
        cinematicAnchor.transform.position = panTo.transform.position;
    }

    void LoadPrefabs()
    {
        //Create a prefabs dictionary
        Prefabs = new Dictionary<string, GameObject>();

        //Load all the prefabs we need for map generation
        Prefabs.Add("floor", Resources.Load<GameObject>("Prefabs/Tiles/Floor"));
        Prefabs["floor"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("floorUp", Resources.Load<GameObject>("Prefabs/Tiles/FloorUp"));
        Prefabs["floorUp"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("floorDown", Resources.Load<GameObject>("Prefabs/Tiles/FloorDown"));
        Prefabs["floorDown"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("floorRight", Resources.Load<GameObject>("Prefabs/Tiles/FloorRight"));
        Prefabs["floorRight"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("floorLeft", Resources.Load<GameObject>("Prefabs/Tiles/FloorLeft"));
        Prefabs["floorLeft"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("special", Resources.Load<GameObject>("Prefabs/Tiles/FloorSpecial"));
        Prefabs["special"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("wall", Resources.Load<GameObject>("Prefabs/Tiles/Wall"));
        Prefabs["wall"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the wall properly
        Prefabs.Add("bronzedoor", Resources.Load<GameObject>("Prefabs/Tiles/BronzeDoor"));
        Prefabs["bronzedoor"].transform.localScale = new Vector3(GridSize / 2.0f, GridSize / 3.0f, 1.0f); //Scale the door properly
        Prefabs.Add("silverdoor", Resources.Load<GameObject>("Prefabs/Tiles/SilverDoor"));
        Prefabs["silverdoor"].transform.localScale = new Vector3(GridSize / 2.0f, GridSize / 3.0f, 1.0f); //Scale the door properly
        Prefabs.Add("golddoor", Resources.Load<GameObject>("Prefabs/Tiles/GoldDoor"));
        Prefabs["golddoor"].transform.localScale = new Vector3(GridSize / 2.0f, GridSize / 3.0f, 1.0f); //Scale the door properly
        Prefabs.Add("bossdoor", Resources.Load<GameObject>("Prefabs/Tiles/BossDoor"));
        Prefabs["bossdoor"].transform.localScale = new Vector3(GridSize / 2.0f, GridSize / 3.0f, 1.0f); //Scale the door properly
        Prefabs.Add("portal", Resources.Load<GameObject>("Prefabs/Tiles/Portal"));
        Prefabs.Add("portalboss", Resources.Load<GameObject>("Prefabs/Tiles/PortalBoss"));
        Prefabs.Add("portalend", Resources.Load<GameObject>("Prefabs/Tiles/PortalEnd"));

        //Load the player prefab
        Prefabs.Add("player", Resources.Load<GameObject>("Prefabs/Player/Player"));

        //Load all the enemy prefabs
        Prefabs.Add("enemy", Resources.Load<GameObject>("Prefabs/Enemies/BaseEnemy"));
        Prefabs.Add("fast", Resources.Load<GameObject>("Prefabs/Enemies/FastEnemy"));
        Prefabs.Add("fastHard", Resources.Load<GameObject>("Prefabs/Enemies/FastEnemyHard"));
        Prefabs.Add("spread", Resources.Load<GameObject>("Prefabs/Enemies/SpreadEnemy"));
        Prefabs.Add("spreadHard", Resources.Load<GameObject>("Prefabs/Enemies/SpreadEnemyHard"));
        Prefabs.Add("tank", Resources.Load<GameObject>("Prefabs/Enemies/TankEnemy"));
        Prefabs.Add("tankHard", Resources.Load<GameObject>("Prefabs/Enemies/TankEnemyHard"));
        Prefabs.Add("ultra", Resources.Load<GameObject>("Prefabs/Enemies/UltraEnemy"));
        Prefabs.Add("ultraHard", Resources.Load<GameObject>("Prefabs/Enemies/UltraEnemyHard"));
        Prefabs.Add("boss", Resources.Load<GameObject>("Prefabs/Enemies/BossEnemyPhase1"));

        //Load all the pick-ups
        Prefabs.Add("heart", Resources.Load<GameObject>("Prefabs/Pickups/HeartPickup"));
        Prefabs.Add("healthboost", Resources.Load<GameObject>("Prefabs/Pickups/HealthBoost"));
        Prefabs.Add("shotboost", Resources.Load<GameObject>("Prefabs/Pickups/ShotBoost"));
        Prefabs.Add("rangeboost", Resources.Load<GameObject>("Prefabs/Pickups/RangeBoost"));
        Prefabs.Add("speedboost", Resources.Load<GameObject>("Prefabs/Pickups/SpeedBoost"));
        Prefabs.Add("bronzekey", Resources.Load<GameObject>("Prefabs/Pickups/BronzeKey"));
        Prefabs.Add("silverkey", Resources.Load<GameObject>("Prefabs/Pickups/SilverKey"));
        Prefabs.Add("goldkey", Resources.Load<GameObject>("Prefabs/Pickups/GoldKey"));

        //Load all the camera and UI prefabs
        Prefabs.Add("camera", Resources.Load<GameObject>("Prefabs/Camera/Main Camera"));
        Prefabs.Add("cameratarget", Resources.Load<GameObject>("Prefabs/Camera/WeightedCameraTarget"));
        Prefabs.Add("cinematicanchor", Resources.Load<GameObject>("Prefabs/Camera/CinematicAnchor"));
        Prefabs.Add("uicanvas", Resources.Load<GameObject>("Prefabs/Player/UICanvas"));
    }
}

public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager TMObject;
    public int seed = 0;

    //Telemetry data
    public int playerShotsFired = 0;
    public int playerDamageTaken = 0;
    public int pickupsCollected = 0;
    public int playerDeaths = 0;
    public int[] enemyKills = new int[6];
    public float distanceTravel = 0.0f;
    public float timeSpent = 0.0f;
    public bool reachedEnd = false;

    private void Awake()
    {
        if (TMObject != null)
        {
            Destroy(gameObject);
            return;
        }

        TMObject = this;

        DontDestroyOnLoad(gameObject);
    }
 
    void Update()
    {
        TMObject.timeSpent += Time.deltaTime;
    }

    public void WriteTelemetry(bool success = false)
    {
        Directory.CreateDirectory(Application.dataPath + "\\Telemetry");

        string filename = DateTime.Now.ToString("MM-dd_tthh.mm.ss");

        if (success) filename = "Success_" + filename;
        else filename = "Failure_" + filename;

        StreamWriter dataStream = new StreamWriter($"{Application.dataPath}\\Telemetry\\Telemetry_{filename}.csv");
        dataStream.WriteLine("Telemetry Data");

        if (TMObject.seed != 0)
            dataStream.WriteLine($"Given Seed, {TMObject.seed}");

        dataStream.WriteLine($"Distance Traveled, {TMObject.distanceTravel}");
        dataStream.WriteLine($"Time Taken, {TMObject.timeSpent}");
        dataStream.WriteLine($"Shots Fired, {TMObject.playerShotsFired}");
        dataStream.WriteLine($"Damage Taken, {TMObject.playerDamageTaken}");
        dataStream.WriteLine($"Pickups Collected, {TMObject.pickupsCollected}");
        dataStream.WriteLine($"Deaths, {TMObject.playerDeaths}");
        dataStream.WriteLine($"Base Enemy Kills, {TMObject.enemyKills[0]}");
        dataStream.WriteLine($"Fast Enemy Kills, {TMObject.enemyKills[1]}");
        dataStream.WriteLine($"Spread Enemy Kills, {TMObject.enemyKills[2]}");
        dataStream.WriteLine($"Tank Enemy Kills, {TMObject.enemyKills[3]}");
        dataStream.WriteLine($"Ultra Enemy Kills, {TMObject.enemyKills[4]}");
        dataStream.WriteLine($"Boss Enemy Kill, {TMObject.enemyKills[5]}");
        if (TMObject.reachedEnd)
            dataStream.WriteLine("Reached End?, TRUE");
        else
            dataStream.WriteLine("Reached End?, FALSE");

        dataStream.Close();
    }

    public void Clear()
    {
        //Reset all Telemetry values
        playerShotsFired = 0;
        playerDamageTaken = 0;
        pickupsCollected = 0;
        playerDeaths = 0;

        for (int i = 0; i < 6; ++i)
            enemyKills[i] = 0;

        distanceTravel = 0.0f;
        timeSpent = 0.0f;
        reachedEnd = false;
    }

    private void OnDestroy()
    {
        if (this == TMObject)
        {
            WriteTelemetry();
        }
    }
}

/// Code from a 32bit Summer Game Jam in June 2024
/// My first experiences with Unity, handled player controller
/// and enemy/obstacle behavior.

public class CreatureBoid : MonoBehaviour
{
    private List<Rigidbody> otherRb;
    private Rigidbody myRb;
    Vector3 position, velocity, spawn;
    public int group;

    //How far they can "see" and the range the avoid other enemies
    public float visualRange = 200f, protectedRange = 50f;
    //
    public float xRestriction = 300f, zRestriction = 300f, yGroundRestriction = 20f, yHeightRestriction = 300f;
    //Boid Variables
    public Vector3 turnFactor = new Vector3(0.7f, 0.7f, 0.7f), centeringFactor = new Vector3(0.0007f, 0.0007f, 0.0007f);

    public Vector3 avoidFactor = new Vector3(0.04f, 0.04f, 0.04f), matchingFactor = new Vector3(0.09f, 0.09f, 0.09f);

    public float maxBias = 0.1f, biasIncrement = 0.00004f, biasVal = 0.001f;
    public float maxSpeed = 50f, minSpeed = 20f;

    void Start()
    {
        spawn = transform.position;
        myRb = GetComponent<Rigidbody>();
        myRb.linearVelocity = new Vector3(Random.Range(-40, 40), Random.Range(-10, 10), Random.Range(-40, 40));
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Boid");
        otherRb = new List<Rigidbody>();
        foreach (GameObject enemy in enemies)
        {
            otherRb.Add(enemy.GetComponent<Rigidbody>());
        }
    }

    void FixedUpdate()
    {
        position = transform.position;
        velocity = myRb.linearVelocity;
        Separation();
        Alignment();
        Cohesion();
        EdgeTurn();
        Bias();
        SpeedLimits();
        myRb.linearVelocity = velocity;
        myRb.rotation = Quaternion.Slerp(myRb.rotation, Quaternion.LookRotation(myRb.linearVelocity), Time.fixedDeltaTime * 0.6f);
    }

    void Separation()
    {
        Vector3 positionOther;
        Vector3 close_d = new Vector3(0f, 0f, 0f);
        foreach (Rigidbody other in otherRb)
        {
            if (other.GetInstanceID() == myRb.GetInstanceID())
            {
                continue;
            }
            positionOther = other.transform.position;
            if (Vector3.Distance(position, positionOther) < protectedRange)
            {
                close_d += position - positionOther;
            }
        }
        velocity += Vector3.Scale(close_d, avoidFactor);
    }
    void Alignment()
    {
        Vector3 vel_avg = new Vector3(0f, 0f, 0f);
        float neighbors = 0f;
        Vector3 velocityOther, positionOther;
        foreach (Rigidbody other in otherRb)
        {
            if (other.GetInstanceID() == myRb.GetInstanceID())
            {
                continue;
            }
            positionOther = other.transform.position;
            if (Vector3.Distance(position, positionOther) < visualRange
                && Vector3.Distance(position, positionOther) > protectedRange)
            {
                velocityOther = other.linearVelocity;
                vel_avg += velocityOther;
                ++neighbors;
            }
        }
        if (neighbors > 0)
        {
            vel_avg /= neighbors;
        }
        velocity += Vector3.Scale((vel_avg - velocity), matchingFactor);
    }

    void Cohesion()
    {
        Vector3 pos_avg = new Vector3(0f, 0f, 0f);
        float neighbors = 0f;
        Vector3 positionOther;
        foreach (Rigidbody other in otherRb)
        {
            if (other.GetInstanceID() == myRb.GetInstanceID())
            {
                continue;
            }
            positionOther = other.transform.position;
            if (Vector3.Distance(position, positionOther) < visualRange
                && Vector3.Distance(position, positionOther) > protectedRange)
            {
                pos_avg += positionOther;
                ++neighbors;
            }
        }
        if (neighbors > 0)
        {
            pos_avg /= neighbors;
        }
        velocity += Vector3.Scale((pos_avg - position), centeringFactor);
    }

    void EdgeTurn()
    {
        if (position.x < spawn.x - xRestriction)
        {
            velocity.x += turnFactor.x;
        }
        else if (position.x > spawn.x + xRestriction)
        {
            velocity.x -= turnFactor.x;
        }
        if (position.z > spawn.z - zRestriction)
        {
            velocity.z -= turnFactor.z;
        }
        else if (position.z < spawn.z + zRestriction)
        {
            velocity.z += turnFactor.z;
        }
        if (position.y < spawn.y + yGroundRestriction)
        {
            velocity.y += turnFactor.y;
        }
        else if (position.y > spawn.y + yHeightRestriction)
        {
            velocity.y -= turnFactor.y;
        }
    }

    void Bias()
    {
        for (int i = 1; i <= 10; ++i)
        {
            if (group == i)
            {
                velocity.x = (Mathf.Pow(-1, i) + biasVal) * velocity.x + (biasVal * (-1 * Mathf.Pow(-1, i)));
            }
        }
    }

    void SpeedLimits()
    {
        float speed = Vector3.Magnitude(velocity);
        if (speed > maxSpeed)
        {
            velocity = (velocity / speed) * maxSpeed;
        }
        if (speed < minSpeed)
        {
            velocity = (velocity / speed) * minSpeed;
        }
    }
}

public class CreatureJump : MonoBehaviour
{
    [SerializeField] private AudioClip[] whaleSoundClips;

    //CreatureJump Stats
    public float flyForce = 400f, jumpForce = 3500f, rotateAcc = 1.2f, detectRange = 180f, ground = 10f, xRestriction = 200f, zRestriction = 200f, gravityForce = 30f;
    //Used for jumping and movement
    private float decisionTime = 8f;
    private Vector3 spawn, direction, newDirection;
    private float timer = 0f;
    private Rigidbody rb;
    public bool canJump = true;
    private bool detected = false, grounded = true, gravity = false;

    //Used to get player location for "detection"
    private GameObject player;

    void Start()
    {
        //Sets random x and z direction, gets it's Rigidbody component and a pointer to the player
        rb = GetComponent<Rigidbody>();
        direction = new Vector3(Random.Range(-30, 30), 0f, Random.Range(-30, 30));
        spawn = transform.position;
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void FixedUpdate()
    {
        Detection();
        Flying();
        if (canJump) Jumping();
        Bounds();
        rb.AddForce(Vector3.Normalize(direction) * flyForce);
        Rotate();
    }

    void Detection()
    {
        Vector3 player3DPos = player.transform.position;
        Vector2 player2DPos = new Vector2(player3DPos.x, player3DPos.z);
        Vector2 my2DPos = new Vector2(transform.position.x, transform.position.z);
        if (Vector2.Distance(my2DPos, player2DPos) < detectRange && player3DPos.y > transform.position.y)
        {
            detected = true;
            if (canJump) newDirection = new Vector3(player3DPos.x - transform.position.x, 0f, player3DPos.z - transform.position.z);
            else newDirection = player3DPos - transform.position;
        }
        else
        {
            detected = false;
        }
    }

    void Flying()
    {
        if (!detected)
        {
            timer += Time.fixedDeltaTime;
            if (timer > decisionTime) newDirection = new Vector3(Random.Range(-40, 40), 0f, Random.Range(-40, 40));
        }
        direction = Vector3.Lerp(direction, newDirection, 0.8f);
    }
    void Jumping()
    {
        if (detected && grounded)
        {
            rb.AddForce(new Vector3(0f, 1f, 0f) * jumpForce, ForceMode.Impulse);
            grounded = false;
            gravity = true;
            SoundFXManager.Instance.PlayRandom3DSoundFXClip(whaleSoundClips, transform.transform, 0.5f);
        }
        if (transform.position.y < spawn.y + ground)
        {
            grounded = true;
            gravity = false;
            transform.position = new Vector3(transform.position.x, spawn.y + ground, transform.position.z);
        }
        if (gravity)
        {
            rb.AddForce(Vector3.down * gravityForce);
        }
    }
    void Bounds()
    {
        if (transform.position.x < spawn.x - xRestriction)
        {
            direction.x += 0.9f;
        }
        else if (transform.position.x > spawn.x + xRestriction)
        {
            direction.x -= 0.9f;
        }
        if (transform.position.z < spawn.z - zRestriction)
        {
            direction.z += 0.9f;
        }
        else if (transform.position.z > spawn.z + zRestriction)
        {
            direction.z -= 0.9f;
        }
    }

    public void Rotate()
    {
        //Rotates the creature in the direction of movement
        rb.rotation = Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(rb.linearVelocity), Time.fixedDeltaTime * rotateAcc);
    }
}

public class AirshipController : MonoBehaviour
{
    [SerializeField] private AudioClip[] damageSoundClips;
    [SerializeField] private AudioClip boostSoundClip;

    public float baseForwardForce = 50f, rollLerp = 0.15f, slowForce = 25f, boostStrength = 10000f, forceCap = 150f;
    private float forwardForce;
    private float pitchSpeed = 2.8f;
    private float maxPitch = 90;
    private float minPitch = -50;


    private float roll = 0;
    private float pitch = 0;
    private float yaw = 0;

    private float HorizontalInput;
    private float VerticalInput;

    private int numBoosts = 3;
    private Coroutine boostTimer;
    private bool isBoostCooldown = false;

    private bool inputToggle = true;
    private Vector3 storedVelocity;

    private Rigidbody rb;

    public UnityEvent onPlayerDeath;
    public UICounter boostCounter;
    public UICounter cargoCounter;
    public Animator animator;
    public LifeCounter lifeCounter;
    public ParticleSystem damageParticleSystem;
    public SkinnedMeshRenderer playerRenderer;

    public void Damaged()
    {
        SoundFXManager.Instance.PlayRandomSoundFXClip(damageSoundClips, transform, 0.5f);
        if (cargoCounter.value > 0) cargoCounter.DecrementValue();
        lifeCounter.LoseHeart();
        animator.SetTrigger("Damage");
        damageParticleSystem.Play();
        if (lifeCounter.GetHeartCount() == 0)
        {
            onPlayerDeath.Invoke();
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        forwardForce = baseForwardForce;
    }

    private void UserInputs()
    {
        float forceMultiplier = forwardForce;

        if (inputToggle)
        {
            HorizontalInput = Input.GetAxis("Horizontal");
            VerticalInput = -Input.GetAxis("Vertical");
            forceMultiplier = GetForceMultiplier();
        }

        boostCounter.SetValue(numBoosts);
        playerRenderer.SetBlendShapeWeight(0, (3f - numBoosts) * 33.3f);
        animator.SetFloat("BlendLR", HorizontalInput);
        animator.SetFloat("BlendFB", VerticalInput);

        rb.AddForce(transform.forward * forceMultiplier);
    }

    private float GetForceMultiplier()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            return slowForce; // slow down
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if (numBoosts > 0)
            {
                SoundFXManager.Instance.PlaySoundFXClip(boostSoundClip, transform, 0.4f);
                animator.SetTrigger("WindBoost");
                if (!isBoostCooldown)
                {
                    isBoostCooldown = true;
                    boostTimer = StartCoroutine(BoostTimer());
                }
                numBoosts--;
                return boostStrength; // speed up
            }

        }

        return forwardForce; // default
    }

    private IEnumerator BoostTimer()
    {
        isBoostCooldown = true;
        yield return new WaitForSeconds(3f);
        numBoosts++;
        if (numBoosts < 3)
        {
            boostTimer = StartCoroutine(BoostTimer());
        }
        else
        {
            isBoostCooldown = false;
        }
    }


    private void Update()
    {
        UserInputs();
    }

    private void FixedUpdate()
    {

        yaw += HorizontalInput * 120 * Time.fixedDeltaTime;

        roll = Mathf.Lerp(0, 30, Mathf.Abs(HorizontalInput)) * -Mathf.Sign(HorizontalInput);

        // Calculate the target pitch based on input and speed
        float targetPitch = pitch + -VerticalInput * pitchSpeed;

        // Clamp the target pitch to the defined range
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        // Smoothly interpolate the pitch value
        pitch = Mathf.Lerp(pitch, targetPitch, pitchSpeed);

        rb.rotation = Quaternion.Slerp(rb.rotation, Quaternion.Euler(Vector3.up * yaw + Vector3.right * pitch + Vector3.forward * roll), rollLerp);
    }
    public void InputOff()
    {
        HorizontalInput = 0;
        VerticalInput = 0;
        inputToggle = false;
    }
    public void InputOn()
    {
        inputToggle = true;
    }
    public void IncreaseForward(float num)
    {
        if (forwardForce < forceCap)
        {
            forwardForce += num;
        }
        else
        {
            forwardForce = forceCap;
        }
    }
    public void DecreaseForward()
    {
        if (forwardForce > baseForwardForce)
        {
            forwardForce -= 10f;
        }
        else
        {
            forwardForce = baseForwardForce;
        }
    }
    public float GetCurrentForward()
    {
        return forwardForce;
    }

    public void StoreVelocity()
    {
        storedVelocity = rb.linearVelocity;
    }

    public void RestoreVelocity()
    {
        rb.linearVelocity = storedVelocity;
    }
}

public class CameraController : MonoBehaviour
{
    public float normalLerp = 0.8f, boostLerp = 0.05f;
    //will be used in speed calculations
    private float currentSpeed;

    private Transform target;
    private AirshipController playerAc;
    private Rigidbody playerRb;

    private void Start()
    {
        playerRb = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody>();
        playerAc = GameObject.FindGameObjectWithTag("Player").GetComponent<AirshipController>();
        target = GameObject.FindGameObjectWithTag("CameraOrigin").transform;
        transform.position = target.position - new Vector3(0f, 0f, 2f);
    }


    private void FixedUpdate()
    {
        // check if distance is too big
        if (Vector3.Distance(transform.position, target.position) > 5)
        {
            Vector3 cameraRelative = target.InverseTransformPoint(transform.position);
            currentSpeed = Mathf.Lerp(currentSpeed, playerRb.linearVelocity.magnitude * 1.15f, boostLerp);
        }
        else
        {
            currentSpeed = playerRb.linearVelocity.magnitude;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.fixedDeltaTime);

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.forward), playerAc.rollLerp + 0.1f);
    }
}

public class WindDraft : MonoBehaviour
{
    [SerializeField] private AudioClip wooshSoundClip;

    //Strength is added on to the ships current speed
    public float draftStrength = 0f, forceAdder = 5f, draftMultiple = 2.5f;
    private float currentDraftStrength;
    public Vector3 direction = new Vector3(0f, 0f, 0f);
    public bool increaseSpeed = true;
    private AirshipController playerAc;
    private Rigidbody playerRb;
    private bool playerEntered;

    private void Start()
    {
        playerAc = GameObject.FindWithTag("Player").GetComponent<AirshipController>();
        playerRb = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        playerEntered = false;
        if (draftStrength == 0f)
        {
            currentDraftStrength = playerAc.baseForwardForce * draftMultiple;
        }
        else
        {
            currentDraftStrength = draftStrength;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent.CompareTag("Player"))
        {
            playerEntered = true;
            SoundFXManager.Instance.PlaySoundFXClip(wooshSoundClip, transform, 0.5f); //play sound fx
            if (increaseSpeed) playerAc.IncreaseForward(forceAdder);
            if (draftStrength == 0f)
            {
                currentDraftStrength = playerAc.GetCurrentForward() * draftMultiple;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.transform.parent.CompareTag("Player"))
        {
            playerEntered = false;
        }

    }
    private void FixedUpdate()
    {
        if (!playerEntered) return;

        if (direction.magnitude == 0)
        {
            playerRb.AddForce(transform.forward * currentDraftStrength);
        }
        else
        {
            playerRb.AddForce(Vector3.Normalize(direction) * currentDraftStrength);
        }
    }
}