using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class playerController : MonoBehaviour, IDamage, Ipickup
{
    [Header("---Components---")]
    [SerializeField] LayerMask ignoreLayer;
    [SerializeField] CharacterController controller;

    [Header("---Stats---")]
    [Range(1, 10)] public int HP;
    [Range(2, 5)][SerializeField] int speed;
    [Range(2, 4)][SerializeField] int sprintMod;
    [Range(5, 20)][SerializeField] int jumpSpeed;
    [Range(1, 3)][SerializeField] int jumpMax;
    [Range(15, 45)][SerializeField] int gravity;
    [SerializeField] int crouchSpeed;
    [SerializeField] float crouchHeight;

    [Header("---Guns---")]
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    int shootDamage;
    int shootDist;
    float shootRate;

    [SerializeField] float dodgeSpeed;
    [SerializeField] float dodgeDuration;
    [SerializeField] float dodgeCooldown;

    [SerializeField] float rollSpeed;
    [SerializeField] float rollDuration;
    [SerializeField] float rollCooldown;

    [SerializeField] AudioSource aud;

    [SerializeField] AudioClip[] audJump;
    [Range(0, 1)][SerializeField] float audJumpVol;
    [SerializeField] AudioClip[] audHurt;
    [Range(0, 1)][SerializeField] float audHurtVol;
    [SerializeField] AudioClip[] audSteps;
    [Range(0, 1)][SerializeField] float audStepsVol;

    [SerializeField] Transform[] teleportDestinations;
    int jumpCount;
    public int HPOrig;
    int gunListPos;
    int originalSpeed;

    float shootTimer;
    float dodgeTimer;
    float dodgeCooldownTimer;

    Vector3 moveDir;
    Vector3 playerVel;

    bool isSprinting;
    bool isCrouching;
    bool isDodging;
    float originalHeight;

    bool isPlayingStep;
    bool isRolling;
    float rollTimer;
    float rollCooldownTimer;


    private bool canTeleport = true;
    private Dictionary<string, Vector3> exitDirections = new Dictionary<string, Vector3>
    {
         { "TeleportSphere1", Vector3.forward },
         { "TeleportSphere2", Vector3.right },
         { "TeleportSphere3", Vector3.left },
         { "TeleportSphere4", Vector3.back }
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HPOrig = HP;
        spawnPlayer();
        originalHeight = controller.height;
        originalSpeed = speed;
        //updatePlayerUI();
    }

    IEnumerator PlayStep()
    {

        isPlayingStep = true;
        aud.PlayOneShot(audSteps[Random.Range(0, audSteps.Length)], audStepsVol);

        if (isSprinting)

            yield return new WaitForSeconds(0.3f);

        else

            yield return new WaitForSeconds(0.5f);

        isPlayingStep = false;
    }

    // Update is called once per frame
    void Update()
    {

        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);

        if (!gamemanager.instance.isPaused)
        {
            movement();

        }

        sprint();

        crouch();

        dodge();

        roll();


    }
    void movement()
    {
        if (controller.isGrounded)
        {
            jumpCount = 0;
            playerVel = Vector3.zero;

        }

        //moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        moveDir = (Input.GetAxis("Horizontal") * transform.right) +
                  (Input.GetAxis("Vertical") * transform.forward);
        //transform.position += moveDir * speed * Time.deltaTime;


        float currentSpeed = isCrouching ? crouchSpeed : speed;

        if (controller.enabled && controller.gameObject.activeInHierarchy)
        {
            controller.Move(moveDir * speed * Time.deltaTime);
            jump();
            playerVel.y -= gravity * Time.deltaTime;
            controller.Move(playerVel * speed * Time.deltaTime);
        }
        shootTimer += Time.deltaTime;

        if (Input.GetButton("Fire1") && gunList.Count > 0 && gunList[gunListPos].ammoCur > 0 && shootTimer >= shootRate)
        {
            shoot();
        }

        selectGun();
        reload();

    }

    void jump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < jumpMax)
        {
            aud.PlayOneShot(audJump[Random.Range(0, audJump.Length)], audJumpVol);
            jumpCount++;
            playerVel.y = jumpSpeed;
        }
    }

    void sprint()
    {
        if (Input.GetButtonDown("Sprint"))
        {
            speed *= sprintMod;
        }
        else if (Input.GetButtonUp("Sprint"))
        {
            speed /= sprintMod;
        }
    }

    void shoot()
    {
        shootTimer = 0;
        gunList[gunListPos].ammoCur--;
        updatePlayerUI();

        if (gunList.Count == 0)
        {
            Debug.LogWarning("No guns available to shoot.");
            return;
        }

        if (gunListPos < 0 || gunListPos >= gunList.Count)
        {
            Debug.LogError($"Invalid gunListPos: {gunListPos}. Resetting to 0.");
            gunListPos = 0;
        }


        if (gunList[gunListPos].shootSound != null && gunList[gunListPos].shootSound.Length > 0)
        {

            aud.PlayOneShot(gunList[gunListPos].shootSound[Random.Range(0, gunList[gunListPos].shootSound.Length)], gunList[gunListPos].shootSoundVol);
        }
        else
        {
            Debug.LogWarning("No shooting sounds available for this gun.");
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, shootDist, ~ignoreLayer))
        {
            Instantiate(gunList[gunListPos].hittEffect, hit.point, Quaternion.identity);
            IDamage dmg = hit.collider.GetComponent<IDamage>();

            if (dmg != null)
            {
                dmg.takeDamage(shootDamage);
            }
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);
        updatePlayerUI();
        StartCoroutine(flashDamageScreen());


        if (HP <= 0)
        {
            // You lose!!
            gamemanager.instance.youlose();
        }
    }

    public void PickupHealthItem(int healthAmount)
    {

        HP += healthAmount;
        HP = Mathf.Clamp(HP, 0, HPOrig);


        updatePlayerUI();
    }


    public void updatePlayerUI()
    {
        gamemanager.instance.playerHPBar.fillAmount = (float)HP / HPOrig;
        if (gunList.Count > 0)
        {

            gamemanager.instance.ammoCur.text = gunList[gunListPos].ammoCur.ToString("F0");
            gamemanager.instance.ammoMax.text = gunList[gunListPos].ammoMax.ToString("F0");
        }
    }
    IEnumerator flashDamageScreen()
    {
        gamemanager.instance.playerDamageScreen.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        gamemanager.instance.playerDamageScreen.SetActive(false);
    }

    public void getGunStats(gunStats gun)
    {
        gunList.Add(gun);
        gunListPos = gunList.Count - 1;
        changeGun();

    }
    void selectGun()

    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && gunListPos < gunList.Count - 1)
        {
            gunListPos++;
            changeGun();

        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && gunListPos > 0)
        {
            gunListPos--;
            changeGun();

        }
    }
    void changeGun()
    {
        shootDamage = gunList[gunListPos].shootDamage;
        shootDist = gunList[gunListPos].shootDist;
        shootRate = gunList[gunListPos].shootRate;

        gunModel.GetComponent<MeshFilter>().sharedMesh = gunList[gunListPos].model.GetComponent<MeshFilter>().sharedMesh;
        gunModel.GetComponent<MeshRenderer>().sharedMaterial = gunList[gunListPos].model.GetComponent<MeshRenderer>().sharedMaterial;

        updatePlayerUI();
    }

    void reload()
    {
        if (Input.GetButtonDown("Reload") && gunList.Count > 0)
        {
            gunList[gunListPos].ammoCur = gunList[gunListPos].ammoMax;
            updatePlayerUI();
        }
    }
    public void spawnPlayer()
    {
        controller.transform.position = gamemanager.instance.playerSpawnPos.transform.position;

        HP = HPOrig;
        updatePlayerUI();
    }

    /// <summary>
    /// EveryThing Below this line was add-on's
    /// </summary>
    void crouch()
    {
        if (Input.GetButtonDown("Crouch"))
        {
            isCrouching = true;
            controller.height = crouchHeight;
            speed = crouchSpeed;
        }
        else if (Input.GetButtonUp("Crouch"))
        {
            isCrouching = false;
            controller.height = originalHeight;
            speed = originalSpeed;
        }

    }

    void dodge()
    {
        if (Input.GetButtonDown("Dodge") && dodgeCooldownTimer >= dodgeCooldown)
        {
            isDodging = true;
            dodgeTimer = 0;
            dodgeCooldownTimer = 0;
        }

        if (isDodging)
        {
            dodgeTimer += Time.deltaTime;
            controller.Move(moveDir * dodgeSpeed * Time.deltaTime);

            if (dodgeTimer >= dodgeDuration)
            {
                isDodging = false;
            }
        }
        dodgeCooldownTimer += Time.deltaTime;
    }

    void roll()
    {
        if (Input.GetButtonDown("Roll") && rollCooldownTimer >= rollCooldown)
        {
            isRolling = true;
            rollTimer = 0;
            rollCooldownTimer = 0;
        }

        if (isRolling)
        {
            rollTimer += Time.deltaTime;
            controller.Move(moveDir * rollSpeed * Time.deltaTime);

            if (rollTimer >= rollDuration)
            {
                isRolling = false;
            }
        }
        rollCooldownTimer += Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered Teleport Sphere: " + other.gameObject.name);
        if (other.CompareTag("TeleportSphere") && canTeleport)
        {
            //Debug.Log("Player entered teleport sphere!" + other.gameObject.name);
            StartCoroutine(TeleportPlayer(other.gameObject.name));
        }
    }
    IEnumerator TeleportPlayer(string teleportSphereTag)
    {
        canTeleport = false;

        controller.enabled = false;
        yield return new WaitForSeconds(0.15f);

        transform.position = teleportDestinations[Random.Range(0, teleportDestinations.Length)].position;

        yield return new WaitForSeconds(0.15f);
        controller.enabled = true;

        if (exitDirections.ContainsKey(teleportSphereTag))
        {
            //transform.position += transform.forward * 3f;
            transform.position += exitDirections[teleportSphereTag] * 3f;
        }
        //controller.Move(moveDir * Time.deltaTime);

        yield return new WaitForSeconds(1f);
        canTeleport = true;
    }

}
