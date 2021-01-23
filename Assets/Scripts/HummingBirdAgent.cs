using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

/// <summary>
/// A humming baird Machine Learning Agent
/// </summary>
public class HummingBirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("torque to apply when rotating")]
    public float torque = 2f;

    [Tooltip("Speed to rotate left and right")]
    public float yawSpeed = 100f;

    [Tooltip("Speed to pitch rotate up or down")]
    public float pitchSpeed = 100f;

    public Camera agentCamera;
    public Transform beakTip;
    public Transform flowerArea;

    [Tooltip("whether this is training mode or gameplay mode")]
    public bool trainingMode;


    private Rigidbody _rigidbody;
    private FlowerArea _flowerArea;
    private Flower _nearestFlower;

    private float _smoothPitchChange;
    private float _smoothYawChange;

    private const float MaxPitchAngle = 80f;

    // maximum distance from the beak tip to accept nectar collision
    private const float BeakTipRadius = 0.008f;

    // Whether the agent is frozen (intentionally not fly)
    private bool frozen = false;

    /// <summary>
    /// The amount of nectar the agent has obtained this episode
    /// </summary>
    public float NectarObtained { get; private set; }

    public override void Initialize()
    {
        Debug.Log("------------  Initialize  -----------");
        //base.Initialize();
        _rigidbody = GetComponent<Rigidbody>();
        _flowerArea = flowerArea.GetComponent<FlowerArea>();

        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    public override void OnEpisodeBegin()
    {
        //base.OnEpisodeBegin();
        if (trainingMode)
        {
            _flowerArea.ResetFlower();
        }

        NectarObtained = 0f;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        bool inFrontOfFlower = true;
        if (trainingMode)
        {
            inFrontOfFlower = Random.value > .5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower);
        UpdateNearestFlower();
    }

    /// <summary>
    /// Called when an action os received from either the player input or neural network
    /// actions.ContinuousActions[i] represents:
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angele (+1 = turn right, -1 = turn left) 
    /// </summary>
    /// <returns></returns>
    public override void OnActionReceived(ActionBuffers actions)
    {
        //base.OnActionReceived(actions);
        if (frozen)
        {
            return;
        }

        var continuousActions = actions.ContinuousActions;
        //var discreteActions = actions.DiscreteActions;

        Vector3 move = new Vector3(continuousActions[0], continuousActions[1], continuousActions[2]);
        //Vector3 rotate = new Vector3(actions.DiscreteActions[3], actions.DiscreteActions[4], 0f);
        _rigidbody.AddForce(move * moveForce);
        //_rigidbody.AddTorque(torque * );

        Vector3 rotationVector = transform.rotation.eulerAngles;
        float pitchChange = continuousActions[3];
        float yawChange = continuousActions[4];

        _smoothPitchChange = Mathf.MoveTowards(_smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        _smoothYawChange = Mathf.MoveTowards(_smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        float pitch = rotationVector.x + _smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + _smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (_nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        //base.CollectObservations(sensor);
        // Oberser the agent's local rotation relative to the island.(4 observation)
        sensor.AddObservation(transform.localRotation.normalized);

        Vector3 toFlower = _nearestFlower.FlowerCenterPosition - beakTip.position;
        sensor.AddObservation(toFlower.normalized); //(3 observation)

        //Where the beak tip is in front of the flower and point toward the flower
        //(+1 = directly in front of the flower, -1 = directly behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -_nearestFlower.FlowerUpVector.normalized));
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -_nearestFlower.FlowerUpVector.normalized));

        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiametor);

        // 10 total observations.
    }

    /// <summary>
    /// When Behavior Type is set to "Heuristic Only" on the agent's Behavior Parameters,
    /// this function will be called. Its return value will be fed into
    /// <see cref="OnActionReceived"/> instead of using the neural network
    ///
    /// in this case player will play the game with keyboard input.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        //base.Heuristic(in actionsOut);
        Vector3 forword = Vector3.zero;
        Vector3 horizontal = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        if (Input.GetKey(KeyCode.W)) forword = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forword = -transform.forward;

        if (Input.GetKey(KeyCode.A)) horizontal = -transform.right;
        else if (Input.GetKey(KeyCode.D)) horizontal = transform.right;

        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        if (Input.GetKey(KeyCode.UpArrow)) pitch = -1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = 1f;

        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        Vector3 combined = (forword + horizontal + up).normalized;

        continuousActionsOut[0] = combined.x;
        continuousActionsOut[1] = combined.y;
        continuousActionsOut[2] = combined.z;

        continuousActionsOut[3] = pitch;
        continuousActionsOut[4] = yaw;
    }

    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not support in training");
        frozen = true;
        _rigidbody.Sleep();
    }

    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not support in training");
        frozen = false;
        _rigidbody.WakeUp();
    }

    private void UpdateNearestFlower()
    {
        foreach (Flower flower in _flowerArea.Flowers)
        {
            if (_nearestFlower == null && flower.HasNectar)
            {
                _nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower =
                    Vector3.Distance(_nearestFlower.transform.position, beakTip.position);
                if (!_nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    _nearestFlower = flower;
                }
            }
        }
    }

    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                Flower randomFlower = _flowerArea.Flowers[Random.Range(0, _flowerArea.Flowers.Count)];

                float distanceFromFlower = Random.Range(.2f, .3f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                float height = Random.Range(1.2f, 2.5f);
                float radius = Random.Range(2f, 7f);
                Quaternion direction = quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);
                potentialPosition = _flowerArea.transform.position + Vector3.up * height +
                                    direction * Vector3.forward * radius;

                float pitch = Random.Range(-60f, 60f);
                float yaw = Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            //check if no colliders are overlapped
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, .05f);
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "can not find a safe position to spawn");
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void TriggerEnterOrStay(Collider collider)
    {
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                Flower flower = _flowerArea.GerFlowerFromNectar(collider);
                float nectarReceived = flower.Feed(.01f);

                NectarObtained += nectarReceived;
                if (trainingMode)
                {
                    float bones = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized,
                        -_nearestFlower.FlowerUpVector.normalized));
                    AddReward(0.1f + bones);
                }

                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (trainingMode && other.collider.CompareTag("boundary"))
        {
            AddReward(-.5f);
        }
    }

    private void Update()
    {
        if (_nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, _nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    private void FixedUpdate()
    {
        if (_nearestFlower != null && !_nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }
}