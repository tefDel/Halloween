using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovErik : MonoBehaviour
{

    public float speedMove;
    public float speedRotation;
    private Animator anim;
    public float x, y;

    public Rigidbody rb;
    public float forceJump=8f;
    public bool canJump;
    // Start is called before the first frame update
    void Start()
    {
        canJump = false;
        anim=GetComponent<Animator>();
    }
    public void FixedUpdate()
    {
        transform.Rotate(0, x * Time.deltaTime * speedRotation, 0);
        transform.Translate(0, 0, y * Time.deltaTime * speedMove);
    }

    // Update is called once per frame
    void Update()
    {

        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        

        anim.SetFloat("velX", x);
        anim.SetFloat("velY", y);

        //Debug.Log("SSSSS " + canJump);
        if (canJump)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                anim.SetBool("jump", true);
                rb.AddForce(new Vector3(0, forceJump, 0), ForceMode.Impulse);
            }
            anim.SetBool("touch_ground", true);
        }
        else
        {
            FallCharacter();
        }
    }

    //personaje esta cayendo 
    public void FallCharacter()
    {
        anim.SetBool("touch_ground", false);
        anim.SetBool("jump", false);
    }
}
