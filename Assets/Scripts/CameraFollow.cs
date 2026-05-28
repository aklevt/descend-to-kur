using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform playerTarget;
    private Transform currentTarget;
    private Vector3 offset = new Vector3(0, 0, -10);

    [Header("Shake Settings")] [SerializeField]
    private float defaultShakeDuration = 0.15f;

    [SerializeField] private float defaultShakeIntensity = 0.1f;

    [Header("Follow Settings")] [SerializeField]
    private float followSpeed = 8f;
    
    [SerializeField] private float defaultZoom = 5f;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 8f;

    [SerializeField] private bool smoothFollow = true;
    [SerializeField] private float uiYOffset = -1.5f;

    [Header("Free Look Settings")] [SerializeField]
    private float freeLookSpeed = 5f;

    [Header("Zoom Settings")] private Vector3 focusOffset;
    private Vector3 shakeOffset;

    private bool isDetached;
    private Vector3 freeLookTarget;
    private Vector2 dragStartMousePos;
    private Vector3 dragStartFreeLookTarget;

    private bool hasBounds;
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private Bounds tilemapBounds;

    private Camera mainCamera;
    private bool isPlayerTurn;

    private Camera MainCamera
    {
        get
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[CameraFollow] MainCamera не найдена");
                }
            }

            return mainCamera;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[CameraFollow] MainCamera не найдена в Awake");
        }
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnStateChanged += OnTurnStateChanged;
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnStateChanged -= OnTurnStateChanged;
    }

    private void OnTurnStateChanged(TurnState state)
    {
        isPlayerTurn = (state == TurnState.PlayerTurn);
        if (!isPlayerTurn)
        {
            ResetToPlayer();
        }
    }

    private void LateUpdate()
    {
        if (currentTarget == null)
            return;

        Vector3 targetPos;

        if (isDetached)
        {
            targetPos = freeLookTarget + offset;
        }
        else
        {
            var finalOffset = offset + focusOffset + new Vector3(0, uiYOffset, 0);
            targetPos = currentTarget.position + finalOffset;
        }

        if (hasBounds)
        {
            targetPos = ClampToBounds(targetPos);
        }

        if (smoothFollow)
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        else
            transform.position = targetPos;

        transform.position += shakeOffset;
    }

    #region PUBLIC API

    /// <summary>
    /// Установить цель для камеры
    /// </summary>
    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
        currentTarget = target;
        Debug.Log($"<color=cyan>[CameraFollow]</color> Цель установлена: {target?.name}");
    }

    /// <summary>
    /// Установить границы перемещения камеры на основе тайлмапа
    /// </summary>
    public void SetCameraBounds(Bounds bounds)
    {
        tilemapBounds = bounds;
        UpdateBoundsWithCurrentZoom(bounds);
    }

    /// <summary>
    /// Убрать ограничения камеры
    /// </summary>
    public void ClearBounds()
    {
        hasBounds = false;
    }

    /// <summary>
    /// Установить динамические горизонтальные границы (для секций/зон внутри комнаты)
    /// </summary>
    public void SetDynamicBoundaries(float leftX, float rightX, float leftOffset = 0f, float rightOffset = 0f)
    {
        var width = (rightX + rightOffset) - (leftX - leftOffset);
        var centerX = (leftX - leftOffset) + width / 2f;

        var sectionBounds = new Bounds(
            new Vector3(centerX, tilemapBounds.center.y, 0),
            new Vector3(width, tilemapBounds.size.y, 100f)
        );

        SetCameraBounds(sectionBounds);

        Debug.Log(
            $"<color=cyan>[CameraFollow]</color> Установлены динамические границы: left={leftX} (offset={leftOffset}), right={rightX} (offset={rightOffset})");
    }

    /// <summary>
    /// Очистить динамические границы
    /// </summary>
    public void ClearDynamicBoundaries()
    {
        hasBounds = false;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        if (!hasBounds) return position;

        return new Vector3(
            Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(position.y, minBounds.y, maxBounds.y),
            position.z
        );
    }

    public void ResetFocus()
    {
        isDetached = false;
        focusOffset = Vector3.zero;
    }

    public void ShiftTowards(Vector3 worldPos, float strength = 0.25f)
    {
        focusOffset = (worldPos - playerTarget.position) * strength;
    }

    #endregion

    /// <summary>
    /// Переместить камеру в нужном направлении (для управления по WASD)
    /// </summary>
    public void MoveFreeLook(Vector2 direction)
    {
        if (!isDetached)
        {
            isDetached = true;
            freeLookTarget = currentTarget.position + focusOffset;
        }

        var movement = new Vector3(direction.x, direction.y, 0f) * freeLookSpeed * Time.deltaTime;
        freeLookTarget += movement;

        if (hasBounds)
        {
            var clampedPos = ClampToBounds(freeLookTarget + offset);
            freeLookTarget = clampedPos - offset;
        }
    }

    public void ResetToPlayer()
    {
        if (playerTarget != null)
        {
            currentTarget = playerTarget;
            ResetFocus();
        }
        else
        {
            Debug.LogWarning("[CameraFollow] playerTarget == null, используется PlayerMovement.Instance");
            if (Entities.PlayerMovement.Instance != null)
            {
                playerTarget = Entities.PlayerMovement.Instance.transform;
                currentTarget = playerTarget;
                ResetFocus();
            }
        }
    }

    public void ShakeLight() => StartCoroutine(ShakeRoutine(defaultShakeIntensity * 0.5f, defaultShakeDuration));
    public void ShakeMedium() => StartCoroutine(ShakeRoutine(defaultShakeIntensity, defaultShakeDuration));
    public void ShakeHeavy() => StartCoroutine(ShakeRoutine(defaultShakeIntensity * 2f, defaultShakeDuration * 1.5f));

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (Time.timeScale > 0f)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                shakeOffset = Random.insideUnitSphere * Mathf.Lerp(intensity, 0f, t);
                shakeOffset.z = 0;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }

            yield return null;
        }

        shakeOffset = Vector3.zero;
    }

    /// <summary>
    /// Изменить зум камеры
    /// </summary>
    public void Zoom(float delta)
    {
        if (MainCamera == null) return;
        mainCamera.orthographicSize = Mathf.Clamp(
            mainCamera.orthographicSize - delta * zoomSpeed,
            minZoom,
            maxZoom
        );
        UpdateBoundsWithCurrentZoom(tilemapBounds);
    }
    
    /// <summary>
    /// Сбросить зум камеры на дефолтное значение
    /// </summary>
    public void ResetZoom()
    {
        if (MainCamera == null) return;
    
        mainCamera.orthographicSize = defaultZoom;
    
        if (hasBounds)
        {
            UpdateBoundsWithCurrentZoom(tilemapBounds);
        }
    }

    /// <summary>
    /// Обновить границы с учётом текущего зума
    /// </summary>
    private void UpdateBoundsWithCurrentZoom(Bounds tilemapBounds)
    {
        var cameraHeight = mainCamera.orthographicSize * 2f;
        var cameraWidth = cameraHeight * mainCamera.aspect;

        minBounds = new Vector3(
            tilemapBounds.min.x + cameraWidth / 2f,
            tilemapBounds.min.y + cameraHeight / 2f,
            offset.z
        );

        maxBounds = new Vector3(
            tilemapBounds.max.x - cameraWidth / 2f,
            tilemapBounds.max.y - cameraHeight / 2f,
            offset.z
        );

        if (minBounds.x > maxBounds.x)
        {
            var center = (tilemapBounds.min.x + tilemapBounds.max.x) / 2f;
            minBounds.x = maxBounds.x = center;
        }

        if (minBounds.y > maxBounds.y)
        {
            var center = (tilemapBounds.min.y + tilemapBounds.max.y) / 2f;
            minBounds.y = maxBounds.y = center;
        }

        hasBounds = true;
    }

    public void SnapToTarget()
    {
        if (currentTarget == null) return;

        var finalOffset = offset + focusOffset + new Vector3(0, uiYOffset, 0);
        var targetPosition = currentTarget.position + finalOffset;

        if (hasBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // Мгновенная телепортация
        transform.position = targetPosition;
        Debug.Log($"<color=cyan>[CameraFollow]</color> SnapToTarget на позицию: {transform.position}");
    }
    
    /// <summary>
    /// Получить базовые границы комнаты (для использования в секциях)
    /// </summary>
    public Bounds GetRoomBounds()
    {
        return tilemapBounds;
    }
}