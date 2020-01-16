﻿// Author: Crayz
// https://youtube.com/crayz92

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public int PlayerLayer = 12;
    public int notPlayerLayerMask = 1 << 12;
    public float MoveSpeed = 7f;
    public float MaxVelocity = 35f;
    public float Gravity = 8;
    public float JumpPower = 5f;
    public float Acceleration = 1f;
    public float AirAcceleration = 1.5f;
    public float StopSpeed = 8f;
    public float Friction = 0.4f;
    public float AirCap = 3f;
    public BoxCollider AABB;
    public Vector3 _newVelocity;
    [SerializeField]
    private bool _grounded;
    [SerializeField]
    private bool _surfing;

    private void Start()
    {
        AABB = GetComponent<BoxCollider>();
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        CheckGrounded();
        CheckJump();

        var inputVector = GetInputVector();
        var wishDir = inputVector.normalized;
        var wishSpeed = inputVector.magnitude;

        if (_grounded)
        {
            ApplyGroundAcceleration(wishDir, wishSpeed, Acceleration, Time.deltaTime, 1f);
            ClampVelocity(MoveSpeed);
            ApplyFriction();
        }
        else
        {
            ApplyAirAcceleration(wishDir, wishSpeed, AirAcceleration, AirCap, Time.deltaTime);
        }

        ClampVelocity(MaxVelocity);

        transform.position += _newVelocity * Time.deltaTime;

        ResolveCollisions();
    }

    private void ApplyGravity()
    {
        if (!_grounded)
        {
            _newVelocity.y -= Gravity * Time.deltaTime;
        }
    }

    private void CheckGrounded()
    {
        _surfing = false;

        var hits = Physics.BoxCastAll(center: AABB.bounds.center,
            halfExtents: transform.localScale,
            direction: Vector3.down,
            orientation: transform.rotation,
            maxDistance: 1
            );
        
        var wasGrounded = _grounded;
        var validHits = hits
            .ToList()
            .FindAll(hit => hit.normal.y >= 0.7f)
            .OrderBy(hit => hit.distance);

        _grounded = validHits.Count() > 0;

        if (_grounded)
        {
            Debug.Log("grounded, hits: " + validHits.Count() + " hitting: " + validHits.First().collider.name);
            var closestHit = validHits.First();
            //if (!wasGrounded)
            //{
            //    //bounce off the ground on first hit
            //    transform.position = new Vector3(transform.position.x, closestHit.point.y + .1, transform.position.z);
            //}

            //If the ground is NOT perfectly flat
            if (closestHit.normal.y < 1)
            {
                Debug.Log("ground not flat");
                ClipVelocity(closestHit.normal, 1.0f);
            }
            else
            {
                _newVelocity.y = 0;
            }
        }
        else
        {
            var surfHits = hits.ToList().FindAll(x => x.normal.y < 0.7f && x.point != Vector3.zero).OrderBy(x => x.distance);
            if (surfHits.Count() > 0)
            {
                transform.position += surfHits.First().normal * 0.02f;
                ClipVelocity(surfHits.First().normal, 1.0f);
                _surfing = true;
            }
        }
    }

    private void CheckJump()
    {
        if (_grounded && Input.GetKey(KeyCode.Space))
        {
            _newVelocity.y += JumpPower;
            _grounded = false;
        }
    }

    private Vector3 GetInputVector()
    {
        var horiz = Input.GetKey(KeyCode.A) ? -MoveSpeed : Input.GetKey(KeyCode.D) ? MoveSpeed : 0;
        var vert = Input.GetKey(KeyCode.S) ? -MoveSpeed : Input.GetKey(KeyCode.W) ? MoveSpeed : 0;
        var inputVelocity = new Vector3(horiz, 0, vert);
        if (inputVelocity.magnitude > MoveSpeed)
        {
            inputVelocity *= MoveSpeed / inputVelocity.magnitude;
        }

        //Get the velocity vector in world space coordinates
        return transform.TransformDirection(inputVelocity);
    }

    private void ApplyGroundAcceleration(Vector3 wishDir, float wishSpeed, float accel, float deltaTime, float surfaceFriction)
    {
        var currentSpeed = Vector3.Dot(_newVelocity, wishDir);
        var addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(accel * deltaTime * wishSpeed * surfaceFriction, addSpeed);
        _newVelocity += accelspeed * wishDir;
    }

    private void ApplyAirAcceleration(Vector3 wishDir, float wishSpeed, float accel, float airCap, float deltaTime)
    {
        var wishSpd = Mathf.Min(wishSpeed, airCap);
        var currentSpeed = Vector3.Dot(_newVelocity, wishDir);
        var addSpeed = wishSpd - currentSpeed;

        if (addSpeed <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(addSpeed, accel * wishSpeed * deltaTime);
        _newVelocity += accelspeed * wishDir;
    }

    private void ApplyFriction()
    {
        var speed = _newVelocity.magnitude;

        if (speed == 0f)
        {
            return;
        }

        var control = (speed < StopSpeed) ? StopSpeed : speed;

        var drop = control * Friction * Time.deltaTime;

        var newSpeed = Mathf.Max(speed - drop, 0);

        if (newSpeed != speed)
        {
            newSpeed /= speed;
            _newVelocity *= newSpeed;
        }
    }

    private void ClampVelocity(float range)
    {
        _newVelocity = Vector3.ClampMagnitude(_newVelocity, MaxVelocity);
    }

    private void ClipVelocity(Vector3 normal, float overbounce)
    {
        var input = _newVelocity;

        // Determine how far along plane to slide based on incoming direction.
        var backoff = Vector3.Dot(input, normal) * overbounce;

        for (int i = 0; i < 3; i++)
        {
            var change = normal[i] * backoff;
            _newVelocity[i] = input[i] - change;
        }

        // iterate once to make sure we aren't still moving through the plane
        var adjust = Vector3.Dot(_newVelocity, normal);
        if (adjust < 0.0f)
        {
            _newVelocity -= (normal * adjust);
        }

        GlobalDebug.Debug("normal: " + normal + " currentVel: " + input + " backoff: " + backoff + " adjust: " + adjust + " newVel: " + _newVelocity);
    }

    private void ResolveCollisions()
    {
        var center = transform.position + AABB.center;
        var overlaps = Physics.OverlapBox(center, AABB.size, Quaternion.identity, notPlayerLayerMask);

        foreach (var other in overlaps)
        {
            if (Physics.ComputePenetration(AABB, AABB.transform.position, AABB.transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 dir, out float dist))
            {
                if (Vector3.Dot(dir, _newVelocity.normalized) > 0)
                {
                    continue;
                }

                Vector3 penetrationVector = dir * dist;

                if (!_surfing)
                {
                    transform.position += penetrationVector;
                    _newVelocity -= Vector3.Project(_newVelocity, -dir);
                }
                else
                {
                    ClipVelocity(dir, 1.0f);
                }
            }
        }
    }

}