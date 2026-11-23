using System.Collections;
using UnityEngine;

public class WeaponM4 : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Animator gunAnimator;
    public CharacterController playerController;
    public DynamicCrosshair crosshair;
    public RecoilController recoilController;
    public CSAmmoHUD ammoHUD;

    [Header("Weapon Stats")]
    public float damage = 33f;
    public float fireRate = 0.09f;
    public float range = 100f;
    public int maxAmmo = 30;
    public int startingMags = 3;
    public bool infiniteReserveMags = true;
    public float reloadTime = 2.2f;

    [Header("CS:GO M4 Recoil Settings")]
    public float recoilMultiplier = 0.8f; // Controls how high bullets climb above crosshair
    public float sprayRecoverySpeed = 10f;
    public float sprayResetTime = 0.35f;
    public float moveSpreadMultiplier = 14f;
    public float airSpread = 28f;

    [Header("Bullet Impacts")]
    public GameObject bulletHolePrefab;
    public GameObject impactSparksPrefab;
    public float decalLifetime = 15f;

    [Header("Muzzle Flash & Tracers")]
    public Transform muzzlePoint;             // Drag MuzzlePoint here
    public Light muzzleLight;                 // Drag MuzzlePoint's Point Light here
    public GameObject bloodOrBotSparksPrefab; // Optional bright red/yellow spark when hitting a bot
    public float tracerDuration = 0.045f;

    [Header("Animation Box Names")]
    public string fireStateName = "Fire";
    public string reloadStateName = "Reload";
    public string reloadEmptyStateName = "Reload_Empty";

    // Authentic CS:GO M4A4 30-shot per-bullet delta (X = Horizontal deg, Y = Vertical deg)
    private readonly Vector2[] m4SprayPattern = new Vector2[]
    {
        new Vector2( 0.00f, 0.00f), // 1  - dead center on crosshair
        new Vector2( 0.02f, 0.42f), // 2  - straight up
        new Vector2(-0.04f, 0.58f), // 3
        new Vector2( 0.05f, 0.72f), // 4
        new Vector2(-0.06f, 0.85f), // 5
        new Vector2( 0.08f, 0.88f), // 6
        new Vector2( 0.16f, 0.78f), // 7  - starts curving right
        new Vector2( 0.28f, 0.58f), // 8
        new Vector2( 0.48f, 0.35f), // 9  - hits ceiling height (~5.4 deg) & breaks right
        new Vector2( 0.65f, 0.18f), // 10 - hard right
        new Vector2( 0.55f, 0.06f), // 11 - flat ceiling
        new Vector2( 0.15f,-0.04f), // 12 - reverses left
        new Vector2(-0.45f,-0.02f), // 13 - sweeps across left
        new Vector2(-0.72f, 0.04f), // 14
        new Vector2(-0.80f, 0.05f), // 15 - far left side of the T
        new Vector2(-0.68f, 0.02f), // 16
        new Vector2(-0.45f,-0.03f), // 17
        new Vector2(-0.15f,-0.02f), // 18 - sweeps back right
        new Vector2( 0.35f, 0.04f), // 19
        new Vector2( 0.62f, 0.05f), // 20
        new Vector2( 0.70f, 0.02f), // 21 - right hook
        new Vector2( 0.50f,-0.03f), // 22
        new Vector2( 0.15f,-0.02f), // 23
        new Vector2(-0.35f, 0.02f), // 24 - settles back left-center
        new Vector2(-0.52f, 0.03f), // 25
        new Vector2(-0.40f,-0.02f), // 26
        new Vector2(-0.10f,-0.02f), // 27
        new Vector2( 0.28f, 0.02f), // 28 - final right hook
        new Vector2( 0.38f, 0.02f), // 29
        new Vector2( 0.22f,-0.01f)  // 30
    };

    private int currentAmmo;
    private int currentMags;
    private int shotIndex = 0;
    private Vector2 accumulatedRecoil = Vector2.zero;
    private float nextTimeToFire = 0f;
    private float lastShotTime = 0f;
    private bool isReloading = false;

    void Start()
    {
        currentAmmo = maxAmmo;
        currentMags = startingMags;
        if (gunAnimator == null) gunAnimator = GetComponent<Animator>();
        RefreshAmmoHUD();
    }

    void Update()
    {
        HandleMovementSpread();

        // Smoothly recover bullet recoil when you stop shooting
        if (!Input.GetButton("Fire1") && Time.time - lastShotTime > sprayResetTime)
        {
            shotIndex = 0;
            accumulatedRecoil = Vector2.Lerp(accumulatedRecoil, Vector2.zero, Time.deltaTime * sprayRecoverySpeed);
        }

        if (isReloading) return;

        bool canReload = infiniteReserveMags || currentMags > 0;
        if (canReload && ((Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo) || (Input.GetButton("Fire1") && currentAmmo <= 0)))
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire && currentAmmo > 0)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    private void HandleMovementSpread()
    {
        if (crosshair == null || playerController == null) return;

        if (!playerController.isGrounded)
        {
            crosshair.currentSpread = Mathf.Max(crosshair.currentSpread, airSpread);
            return;
        }

        Vector3 horizontalVel = new Vector3(playerController.velocity.x, 0, playerController.velocity.z);
        // Threshold set to 2.9f so Shift-walking (2.8f) stays accurate just like CS:GO
        if (horizontalVel.magnitude > 2.9f)
        {
            crosshair.currentSpread = Mathf.Max(crosshair.currentSpread, moveSpreadMultiplier);
        }
    }

    private void Shoot()
    {
        currentAmmo--;
        lastShotTime = Time.time;
        RefreshAmmoHUD();

        if (gunAnimator != null && !string.IsNullOrEmpty(fireStateName))
        {
            gunAnimator.Play(fireStateName, 0, 0f);
        }

        if (muzzleLight != null)
        {
            StartCoroutine(FlashMuzzleLight());
        }

        // Get this bullet's step in the CS:GO M4 pattern
        Vector2 step = m4SprayPattern[Mathf.Clamp(shotIndex, 0, m4SprayPattern.Length - 1)] * recoilMultiplier;
        shotIndex++;
        accumulatedRecoil += step;

        // Subtle camera viewpunch
        if (recoilController != null)
        {
            recoilController.AddRecoil(step.y * 2f, step.x * 2f);
        }

        // Aim bullet ABOVE & SIDEWAYS from the crosshair based on accumulated recoil
        Quaternion recoilRotation = Quaternion.Euler(-accumulatedRecoil.y, accumulatedRecoil.x, 0f);
        Vector3 shootDirection = playerCamera.transform.rotation * recoilRotation * Vector3.forward;

        // Add random inaccuracy only when moving, jumping, or deep into a spray
        if (crosshair != null && (shotIndex > 1 || crosshair.currentSpread > 1f))
        {
            float spread = crosshair.GetSpreadAngle();
            shootDirection += playerCamera.transform.right * Random.Range(-spread, spread);
            shootDirection += playerCamera.transform.up * Random.Range(-spread, spread);
            shootDirection.Normalize();
        }

        Vector3 startPoint = (muzzlePoint != null) ? muzzlePoint.position : playerCamera.transform.position;
        Vector3 endPoint = playerCamera.transform.position + shootDirection * range;

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, shootDirection, out hit, range))
        {
            endPoint = hit.point;

            // Check if we hit a bot's body or head hitbox
            BotHitbox hitbox = hit.transform.GetComponent<BotHitbox>();
            if (hitbox != null)
            {
                hitbox.OnHit(damage);

                GameObject botSpark = (bloodOrBotSparksPrefab != null) ? bloodOrBotSparksPrefab : impactSparksPrefab;
                if (botSpark != null)
                {
                    GameObject sparks = Instantiate(botSpark, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(sparks, 0.8f);
                }
            }
            else
            {
                // Only spawn wall decals on environment surfaces, not moving bots
                if (bulletHolePrefab != null)
                {
                    Vector3 decalPos = hit.point + (hit.normal * 0.005f);
                    Quaternion decalRot = Quaternion.LookRotation(-hit.normal);
                    GameObject decal = Instantiate(bulletHolePrefab, decalPos, decalRot);
                    Destroy(decal, decalLifetime);
                }

                if (impactSparksPrefab != null)
                {
                    GameObject sparks = Instantiate(impactSparksPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(sparks, 1f);
                }
            }
        }

        StartCoroutine(SpawnPlayerTracer(startPoint, endPoint));

        if (crosshair != null)
        {
            crosshair.AddSpread(step.y * 3.5f);
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        shotIndex = 0;
        accumulatedRecoil = Vector2.zero;
        RefreshAmmoHUD();

        string animToPlay = (currentAmmo <= 0) ? reloadEmptyStateName : reloadStateName;
        if (gunAnimator != null && !string.IsNullOrEmpty(animToPlay))
        {
            gunAnimator.Play(animToPlay, 0, 0f);
        }

        yield return new WaitForSeconds(reloadTime);

        if (!infiniteReserveMags)
        {
            currentMags = Mathf.Max(0, currentMags - 1);
        }

        currentAmmo = maxAmmo;
        isReloading = false;
        RefreshAmmoHUD();
    }

    private void RefreshAmmoHUD()
    {
        if (ammoHUD != null)
        {
            ammoHUD.UpdateHUD(currentAmmo, maxAmmo, currentMags, isReloading);
        }
    }

    private IEnumerator FlashMuzzleLight()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(0.04f);
        muzzleLight.enabled = false;
    }

    private IEnumerator SpawnPlayerTracer(Vector3 start, Vector3 end)
    {
        GameObject tracer = new GameObject("PlayerTracer");
        LineRenderer lr = tracer.AddComponent<LineRenderer>();
        lr.startWidth = 0.02f;
        lr.endWidth = 0.008f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.92f, 0.55f, 0.85f);
        lr.endColor = new Color(1f, 0.7f, 0.2f, 0.15f);

        yield return new WaitForSeconds(tracerDuration);
        Destroy(tracer);
    }
}