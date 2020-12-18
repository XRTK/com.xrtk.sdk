// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.Rendering;
using XRTK.EventDatum.Teleport;
using XRTK.Extensions;
using XRTK.Interfaces.TeleportSystem;

namespace XRTK.SDK.TeleportSystem
{
    /// <summary>
    /// This <see cref="IMixedRealityTeleportSystem"/> handler implementation will
    /// fade out the camera when teleporting and fade it back in when done.
    /// </summary>
    [System.Runtime.InteropServices.Guid("0db5b0fd-9ac3-487a-abfd-754963f4e2a3")]
    public class FadingTeleportHandler : BaseTeleportHandler
    {
        [SerializeField]
        [Tooltip("Assign the transform with the camera component attached. If not set, the component uses its own transform.")]
        private Transform cameraTransform = null;

        [SerializeField]
        [Tooltip("Assign the transform being teleported to the target location. If not set, the component game object's parent transform is used.")]
        private Transform teleportTransform = null;

        [SerializeField]
        [Tooltip("Duration of the fade in / fade out in seconds.")]
        private float fadeDuration = .25f;

        private Vector3 targetPosition;
        private Vector3 targetRotation;
        private TeleportEventData teleportEventData;
        private GameObject fadeSphere;
        private MeshRenderer fadeSphereRenderer;
        private Color fadeInColor = Color.clear;
        private Color fadeOutColor = Color.black;
        private bool isFadingOut;
        private bool isFadingIn;
        private float fadeTime;

        /// <summary>
        /// Awake is called when the instance is being loaded.
        /// </summary>
        private void Awake()
        {
            if (cameraTransform.IsNull())
            {
                cameraTransform = transform;
            }

            if (teleportTransform.IsNull())
            {
                teleportTransform = cameraTransform.parent;
                Debug.Assert(teleportTransform != null,
                    $"{nameof(InstantTeleportHandler)} requires that the camera be parented under another object " +
                    $"or a parent transform was assigned in editor.");
            }

            InitiailzeFadeSphere();
        }

        /// <summary>
        /// Update is called every frame, if the behaviour is enabled.
        /// </summary>
        private void Update()
        {
            if (isFadingOut)
            {
                fadeTime += Time.deltaTime;
                var t = fadeTime / fadeDuration;
                var frameColor = Color.Lerp(fadeInColor, fadeOutColor, t);
                var material = fadeSphereRenderer.material;
                material.color = frameColor;
                fadeSphereRenderer.material = material;

                if (t >= 1f)
                {
                    isFadingOut = false;
                    PerformTeleport();
                }
            }
            else if (isFadingIn)
            {
                fadeTime += Time.deltaTime;
                var t = fadeTime / fadeDuration;
                var frameColor = Color.Lerp(fadeOutColor, fadeInColor, t);
                var material = fadeSphereRenderer.material;
                material.color = frameColor;
                fadeSphereRenderer.material = material;

                if (t >= 1f)
                {
                    isFadingIn = false;
                    fadeSphere.SetActive(false);
                }
            }
        }

        /// <inheritdoc />
        public override void OnTeleportCanceled(TeleportEventData eventData) => fadeSphere.SetActive(false);

        /// <inheritdoc />
        public override void OnTeleportCompleted(TeleportEventData eventData) => FadeIn();

        /// <inheritdoc />
        public override void OnTeleportStarted(TeleportEventData eventData)
        {
            if (eventData.used)
            {
                return;
            }

            eventData.Use();

            teleportEventData = eventData;
            targetRotation = Vector3.zero;
            targetPosition = eventData.Pointer.Result.EndPoint;
            targetRotation.y = eventData.Pointer.PointerOrientation;

            if (eventData.HotSpot != null)
            {
                targetPosition = eventData.HotSpot.Position;
                if (eventData.HotSpot.OverrideTargetOrientation)
                {
                    targetRotation.y = eventData.HotSpot.TargetOrientation;
                }
            }

            FadeOut();
        }

        private void PerformTeleport()
        {
            var height = targetPosition.y;
            targetPosition -= cameraTransform.position - teleportTransform.position;
            targetPosition.y = height;
            teleportTransform.position = targetPosition;
            teleportTransform.RotateAround(cameraTransform.position, Vector3.up, targetRotation.y - cameraTransform.eulerAngles.y);

            TeleportSystem.RaiseTeleportComplete(teleportEventData.Pointer, teleportEventData.HotSpot);
        }

        private void FadeOut()
        {
            fadeSphere.SetActive(true);
            fadeTime = 0f;
            isFadingIn = false;
            isFadingOut = true;
        }

        private void FadeIn()
        {
            fadeSphere.SetActive(true);
            fadeTime = 0f;
            isFadingOut = false;
            isFadingIn = true;
        }

        private void InitiailzeFadeSphere()
        {
            if (fadeSphere.IsNull())
            {
                // We use a simple sphere around the camera / head, which
                // we can fade in/out to simulate the camera fading to black.
                fadeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fadeSphere.name = $"{nameof(FadingTeleportHandler)}_Fade";
                fadeSphere.transform.SetParent(cameraTransform);
                fadeSphere.transform.localPosition = Vector3.zero;
                fadeSphere.transform.localRotation = Quaternion.identity;
                fadeSphere.transform.localScale = new Vector3(.5f, .5f, .5f);
                Destroy(fadeSphere.GetComponent<SphereCollider>());

                // Invert the sphere normals to point inwards
                // (towards camera, so we can see the darkness in our life).
                var meshFilter = fadeSphere.GetComponent<MeshFilter>();
                var normals = meshFilter.mesh.normals;
                var triangles = meshFilter.mesh.triangles;
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = -normals[i];
                }

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var t = triangles[i];
                    triangles[i] = triangles[i + 2];
                    triangles[i + 2] = t;
                }

                meshFilter.mesh.normals = normals;
                meshFilter.mesh.triangles = triangles;

                // Configure the mesh renderer to not impact anything else
                // in the scene.
                fadeSphereRenderer = fadeSphere.GetComponent<MeshRenderer>();
                fadeSphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fadeSphereRenderer.receiveShadows = false;
                fadeSphereRenderer.allowOcclusionWhenDynamic = false;
                fadeSphereRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                fadeSphereRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                // Finally paint the sphere black. We use the default
                // material created on the sphere to clone its properties
                // and paint it black with transparency enabled.
                var blackMaterial = new Material(fadeSphereRenderer.material)
                {
                    color = fadeOutColor
                };

                if (GraphicsSettings.renderPipelineAsset.IsNull())
                {
                    blackMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
                    blackMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    blackMaterial.SetInt("_ZWrite", 0);
                    blackMaterial.DisableKeyword("_ALPHATEST_ON");
                    blackMaterial.DisableKeyword("_ALPHABLEND_ON");
                    blackMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    blackMaterial.renderQueue = 3000;
                }
                else
                {
                    Debug.LogError($"{nameof(FadingTeleportHandler)} does not support render pipelines. The handler won't be able to fade in and out.");
                }

                fadeSphereRenderer.material = blackMaterial;
            }

            // Initially hide the sphere, we only want it to be active when
            // fading.
            fadeSphere.SetActive(false);
        }
    }
}
