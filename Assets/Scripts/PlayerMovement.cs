using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //引用的外部组件
    public Animator animator;

    //角色可设置属性
    public float moveSpeed = 5f; // 正常移动速度

    //角色内部变量
    private float currentSpeed;
    private Vector2 moveDirection;

    void Start()
    {
        animator = GetComponent<Animator>();
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        float horizontal = ArduinoController.Instance.GetHorizontalInput();
        animator.SetBool("isMoving", horizontal != 0);
        moveDirection = new Vector2(horizontal, 0).normalized;

        // 保留向左走时旋转180度
        if (horizontal < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (horizontal > 0)
            transform.localScale = new Vector3(1, 1, 1);

        MovePlayer();
    }

    private void MovePlayer()
    {
        currentSpeed = moveSpeed;
        transform.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);
    }
}