using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Extensions.PerformanceSettings;

namespace Anaglyph.XRTemplate
{
	[DefaultExecutionOrder(500)]
	public class XRConfigurer : MonoBehaviour
	{
		//[SerializeField] private float renderScale = 1.0f;
		[SerializeField] private float framerateTarget = 72f;

		[SerializeField] private PerformanceLevelHint suggestedCpuPerfLevel = PerformanceLevelHint.SustainedHigh;
		[SerializeField] private PerformanceLevelHint suggestedGpuPerfLevel = PerformanceLevelHint.SustainedHigh;

		//[Header("Should only be 0, 2, 4, or 8!")]
		//[SerializeField] private ushort antialiasingMsaa = 4;

		void Start()
		{
			XrPerformanceSettingsFeature.SetPerformanceLevelHint(PerformanceDomain.Cpu,
				suggestedCpuPerfLevel);
			XrPerformanceSettingsFeature.SetPerformanceLevelHint(PerformanceDomain.Gpu,
				suggestedGpuPerfLevel);
		}
	}
}
