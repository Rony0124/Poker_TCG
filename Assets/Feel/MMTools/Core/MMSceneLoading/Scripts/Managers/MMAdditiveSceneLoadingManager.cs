﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace MoreMountains.Tools
{	
	[System.Serializable]
	public class ProgressEvent : UnityEvent<float>{}

	/// <summary>
	/// A simple class used to store additive loading settings
	///
	/// It provides a nice load sequence, entirely customizable, here's the high level sequence breakdown:
	/// - delay before entry fade
	/// - entry fade
	/// - delay after entry fade
	/// - unload origin scene
	/// - load destination scene
	/// - load complete
	/// - delay before scene activation
	/// - scene activation
	/// - delay after scene activation
	/// - exit fade
	/// - unload scene loader
	/// </summary>
	[Serializable]
	public class MMAdditiveSceneLoadingManagerSettings
	{
		/// the possible ways to unload scenes
		public enum UnloadMethods { None, ActiveScene, AllScenes };
		/// the name of the MMSceneLoadingManager scene you want to use when in additive mode
		[Tooltip("the name of the MMSceneLoadingManager scene you want to use when in additive mode")]
		public string LoadingSceneName = "MMAdditiveLoadingScreen";
		/// when in additive loading mode, the thread priority to apply to the loading
		[Tooltip("when in additive loading mode, the thread priority to apply to the loading")]
		public ThreadPriority ThreadPriority = ThreadPriority.High;
		/// whether or not to make additional sanity checks (better leave this to true)
		[Tooltip("whether or not to make additional sanity checks (better leave this to true)")]
		public bool SecureLoad = true;
		/// when in additive loading mode, whether or not to interpolate the progress bar's progress
		[Tooltip("when in additive loading mode, whether or not to interpolate the progress bar's progress")]
		public bool InterpolateProgress = true;
		/// when in additive loading mode, when in additive loading mode, the duration (in seconds) of the delay before the entry fade
		[Tooltip("when in additive loading mode, when in additive loading mode, the duration (in seconds) of the delay before the entry fade")]
		public float BeforeEntryFadeDelay = 0f;
		/// when in additive loading mode, the duration (in seconds) of the entry fade
		[Tooltip("when in additive loading mode, the duration (in seconds) of the entry fade")]
		public float EntryFadeDuration = 0.25f;
		/// when in additive loading mode, the duration (in seconds) of the delay before the entry fade
		[Tooltip("when in additive loading mode, the duration (in seconds) of the delay before the entry fade")]
		public float AfterEntryFadeDelay = 0.1f;
		/// when in additive loading mode, the duration (in seconds) of the delay before the scene gets activated
		[Tooltip("when in additive loading mode, the duration (in seconds) of the delay before the scene gets activated")]
		[FormerlySerializedAs("BeforeExitFadeDelay")] 
		public float BeforeSceneActivationDelay = 0.25f;
		/// when in additive loading mode, the duration (in seconds) after the scene is loaded and before the fade starts
		[Tooltip("when in additive loading mode, the duration (in seconds) after the scene is loaded and before the fade starts")]
		public float AfterSceneActivationDelay = 0f;
		/// when in additive loading mode, the duration (in seconds) of the exit fade
		[Tooltip("when in additive loading mode, the duration (in seconds) of the exit fade")]
		public float ExitFadeDuration = 0.2f;
		/// when in additive loading mode, when in additive loading mode, the tween to use to fade on entry
		[Tooltip("when in additive loading mode, when in additive loading mode, the tween to use to fade on entry")]
		public MMTweenType EntryFadeTween = null;
		/// when in additive loading mode, the tween to use to fade on exit
		[Tooltip("when in additive loading mode, the tween to use to fade on exit")]
		public MMTweenType ExitFadeTween = null;
		/// when in additive loading mode, the speed at which the loader's progress bar should move
		[Tooltip("when in additive loading mode, the speed at which the loader's progress bar should move")]
		public float ProgressBarSpeed = 5f;
		/// a list of progress intervals (values should be between 0 and 1) and their associated speeds, letting you have the bar progress less linearly
		[Tooltip("a list of progress intervals (values should be between 0 and 1) and their associated speeds, letting you have the bar progress less linearly")]
		public List<MMSceneLoadingSpeedInterval> SpeedIntervals;
		/// whether or not to display debug logs of the loading sequence
		[Tooltip("whether or not to display debug logs of the loading sequence")]
		public bool DebugMode = false;
		/// when in additive loading mode, the selective additive fade mode
		[Tooltip("when in additive loading mode, the selective additive fade mode")]
		public MMAdditiveSceneLoadingManager.FadeModes FadeMode = MMAdditiveSceneLoadingManager.FadeModes.FadeInThenOut;
		/// the chosen way to unload scenes (none, only the active scene, all loaded scenes)
		[Tooltip("the chosen way to unload scenes (none, only the active scene, all loaded scenes)")]
		public UnloadMethods UnloadMethod = UnloadMethods.AllScenes;
		/// the name of the anti spill scene to use when loading additively.
		/// If left empty, that scene will be automatically created, but you can specify any scene to use for that. Usually you'll want your own anti spill scene to be just an empty scene, but you can customize its lighting settings for example.
		[Tooltip("the name of the anti spill scene to use when loading additively." +
		         "If left empty, that scene will be automatically created, but you can specify any scene to use for that. Usually you'll want your own anti spill scene to be just an empty scene, but you can customize its lighting settings for example.")]
		public string AntiSpillSceneName = "";
	}

	/// <summary>
	/// A class used to define different interpolation speeds for specific progress intervals
	/// </summary>
	[Serializable]
	public class MMSceneLoadingSpeedInterval
	{
		/// The progress interval (between 0 and 1) 
		public MMInterval<float> Interval;
		/// the speed at which the bar should move on that interval
		public float Speed = 1f;
	}
	
	/// <summary>
	/// A class to load scenes using a loading screen instead of just the default API
	/// This is a new version of the classic LoadingSceneManager (now renamed to MMSceneLoadingManager for consistency)
	/// </summary>
	public class MMAdditiveSceneLoadingManager : MMMonoBehaviour 
	{
		/// The possible orders in which to play fades (depends on the fade you've set in your loading screen
		public enum FadeModes { FadeInThenOut, FadeOutThenIn }
		public enum HoldModes { AfterEntryFade, AfterUnloadOriginScene, BeforeSceneActivation, BeforeExitFade }
		
		[MMInspectorGroup("Audio Listener", true, 3)]
		public AudioListener LoadingAudioListener;
		
		[MMInspectorGroup("Settings", true, 10)]
		/// the ID on which to trigger a fade, has to match the ID on the fader in your scene
		[Tooltip("the ID on which to trigger a fade, has to match the ID on the fader in your scene")]
		public int FaderID = 500;
		/// whether or not to output debug messages to the console
		[Tooltip("whether or not to output debug messages to the console")]
		public static bool DebugMode = false;

		[MMInspectorGroup("Progress Events", true, 11)]
		/// an event used to update progress 
		[Tooltip("an event used to update progress")]
		public ProgressEvent SetRealtimeProgressValue;
		/// an event used to update progress with interpolation
		[Tooltip("an event used to update progress with interpolation")]
		public ProgressEvent SetInterpolatedProgressValue;

		[MMInspectorGroup("State Events", true, 12)]
		/// an event that will be invoked when the load starts
		[Tooltip("an event that will be invoked when the load starts")]
		public UnityEvent OnLoadStarted;
		/// an event that will be invoked when the delay before the entry fade starts
		[Tooltip("an event that will be invoked when the delay before the entry fade starts")]
		public UnityEvent OnBeforeEntryFade;
		/// an event that will be invoked when the entry fade starts
		[Tooltip("an event that will be invoked when the entry fade starts")]
		public UnityEvent OnEntryFade;
		/// an event that will be invoked when the delay after the entry fade starts
		[Tooltip("an event that will be invoked when the delay after the entry fade starts")]
		public UnityEvent OnAfterEntryFade;
		/// an event that will be invoked when the origin scene gets unloaded
		[Tooltip("an event that will be invoked when the origin scene gets unloaded")]
		public UnityEvent OnUnloadOriginScene;
		/// an event that will be invoked when the destination scene starts loading
		[Tooltip("an event that will be invoked when the destination scene starts loading")]
		public UnityEvent OnLoadDestinationScene;
		/// an event that will be invoked when the load of the destination scene is complete
		[Tooltip("an event that will be invoked when the load of the destination scene is complete")]
		public UnityEvent OnLoadProgressComplete;
		/// an event that will be invoked when the interpolated load of the destination scene is complete
		[Tooltip("an event that will be invoked when the interpolated load of the destination scene is complete")]
		public UnityEvent OnInterpolatedLoadProgressComplete;
		/// an event that will be invoked when the delay before scene activation starts
		[Tooltip("an event that will be invoked when the delay before scene activation starts")]
		[FormerlySerializedAs("OnBeforeExitFade")] 
		public UnityEvent OnBeforeSceneActivation;
		/// an event that will be invoked after the scene has been activated
		[Tooltip("an event that will be invoked after the scene has been activated")]
		public UnityEvent OnAfterSceneActivation;
		/// an event that will be invoked when the exit fade starts
		[Tooltip("an event that will be invoked when the exit fade starts")]
		public UnityEvent OnExitFade;
		/// an event that will be invoked when the destination scene gets activated
		[Tooltip("an event that will be invoked when the destination scene gets activated")]
		public UnityEvent OnDestinationSceneActivation;
		/// an event that will be invoked when the scene loader gets unloaded
		[Tooltip("an event that will be invoked when the scene loader gets unloaded")]
		public UnityEvent OnUnloadSceneLoader;

		protected static bool _interpolateProgress;
		protected static float _progressInterpolationSpeed;
		protected static List<MMSceneLoadingSpeedInterval> _speedIntervals;
		protected static float _beforeEntryFadeDelay;
		protected static MMTweenType _entryFadeTween;
		protected static float _entryFadeDuration;
		protected static float _afterEntryFadeDelay;
		protected static float _beforeSceneActivationDelay;
		protected static float _afterSceneActivationDelay;
		protected static MMTweenType _exitFadeTween;
		protected static float _exitFadeDuration;
		protected static FadeModes _fadeMode;
		protected static string _sceneToLoadName = "";
		protected static string _loadingScreenSceneName;
		protected static List<string> _scenesInBuild;
		protected static Scene[] _initialScenes;
		protected float _loadProgress = 0f;
		protected float _interpolatedLoadProgress;
		protected static bool _loadingInProgress = false;
		protected AsyncOperation _unloadOriginAsyncOperation;
		protected AsyncOperation _loadDestinationAsyncOperation;
		protected AsyncOperation _unloadLoadingAsyncOperation;
		protected bool _setRealtimeProgressValueIsNull;
		protected bool _setInterpolatedProgressValueIsNull;
		protected const float _asyncProgressLimit = 0.9f;
		protected MMSceneLoadingAntiSpill _antiSpill = new MMSceneLoadingAntiSpill();
		protected static string _antiSpillSceneName = "";
		
		protected static Dictionary<HoldModes, bool> _holds = new Dictionary<HoldModes, bool>
		{
			{HoldModes.AfterEntryFade, false},
			{HoldModes.AfterUnloadOriginScene, false},
			{HoldModes.BeforeSceneActivation, false},
			{HoldModes.BeforeExitFade, false}
		};
		
		/// <summary>
		/// Statics initialization to support enter play modes
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		protected static void InitializeStatics()
		{
			_loadingInProgress = false;
			_interpolateProgress = false;
			_progressInterpolationSpeed = 0f;
			_speedIntervals = new List<MMSceneLoadingSpeedInterval>();
			_beforeEntryFadeDelay = 0f;
			_entryFadeTween = null;
			_entryFadeDuration = 0f;
			_afterEntryFadeDelay = 0f;
			_beforeSceneActivationDelay = 0f;
			_afterSceneActivationDelay = 0f;
			_exitFadeTween = null;
			_exitFadeDuration = 0f;
			_sceneToLoadName = "";
			_loadingScreenSceneName = "";
			_scenesInBuild = new List<string>();
			_initialScenes = null;
			_antiSpillSceneName = "";
		}

		/// <summary>
		/// Call this static method to load a scene from anywhere (packed settings signature)
		/// </summary>
		/// <param name="sceneToLoadName"></param>
		/// <param name="settings"></param>
		public static void LoadScene(string sceneToLoadName, MMAdditiveSceneLoadingManagerSettings settings)
		{
			LoadScene(sceneToLoadName, settings.LoadingSceneName, settings.ThreadPriority, settings.SecureLoad, settings.InterpolateProgress,
				settings.BeforeEntryFadeDelay, settings.EntryFadeDuration, settings.AfterEntryFadeDelay, settings.BeforeSceneActivationDelay, settings.AfterSceneActivationDelay,
				settings.ExitFadeDuration, settings.EntryFadeTween, settings.ExitFadeTween, settings.ProgressBarSpeed, settings.FadeMode, settings.UnloadMethod, settings.AntiSpillSceneName,
				settings.SpeedIntervals, settings.DebugMode);
		}
        
		/// <summary>
		/// Call this static method to load a scene from anywhere
		/// </summary>
		/// <param name="sceneToLoadName">Level name.</param>
		public static void LoadScene(string sceneToLoadName, string loadingSceneName = "MMAdditiveLoadingScreen", 
			ThreadPriority threadPriority = ThreadPriority.High, bool secureLoad = true,
			bool interpolateProgress = true,
			float beforeEntryFadeDelay = 0f,
			float entryFadeDuration = 0.25f,
			float afterEntryFadeDelay = 0.1f,
			float beforeSceneActivationDelay = 0.25f,
			float afterSceneActivationDelay = 0f,
			float exitFadeDuration = 0.2f, 
			MMTweenType entryFadeTween = null, MMTweenType exitFadeTween = null,
			float progressBarSpeed = 5f, 
			FadeModes fadeMode = FadeModes.FadeInThenOut,
			MMAdditiveSceneLoadingManagerSettings.UnloadMethods unloadMethod = MMAdditiveSceneLoadingManagerSettings.UnloadMethods.AllScenes,
			string antiSpillSceneName = "",
			List<MMSceneLoadingSpeedInterval> speedIntervals = null,
			bool debugMode = false)
		{
			if (_loadingInProgress)
			{
				Debug.LogError("MMLoadingSceneManagerAdditive : a request to load a new scene was emitted while a scene load was already in progress");  
				return;
			}

			if (entryFadeTween == null)
			{
				entryFadeTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic);
			}

			if (exitFadeTween == null)
			{
				exitFadeTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic);
			}

			if (secureLoad)
			{
				_scenesInBuild = MMScene.GetScenesInBuild();
	            
				if (!_scenesInBuild.Contains(sceneToLoadName))
				{
					Debug.LogError("MMLoadingSceneManagerAdditive : impossible to load the '"+sceneToLoadName+"' scene, " +
					               "there is no such scene in the project's build settings.");
					return;
				}
				if (!_scenesInBuild.Contains(loadingSceneName))
				{
					Debug.LogError("MMLoadingSceneManagerAdditive : impossible to load the '"+loadingSceneName+"' scene, " +
					               "there is no such scene in the project's build settings.");
					return;
				}
			}

			_loadingInProgress = true;
			_initialScenes = GetScenesToUnload(unloadMethod);

			Application.backgroundLoadingPriority = threadPriority;
			DebugMode = debugMode;
			_sceneToLoadName = sceneToLoadName;					
			_loadingScreenSceneName = loadingSceneName;
			_beforeEntryFadeDelay = beforeEntryFadeDelay;
			_entryFadeDuration = entryFadeDuration;
			_entryFadeTween = entryFadeTween;
			_afterEntryFadeDelay = afterEntryFadeDelay;
			_progressInterpolationSpeed = progressBarSpeed;
			_beforeSceneActivationDelay = beforeSceneActivationDelay;
			_exitFadeDuration = exitFadeDuration;
			_afterSceneActivationDelay = afterSceneActivationDelay;
			_exitFadeTween = exitFadeTween;
			_fadeMode = fadeMode;
			_interpolateProgress = interpolateProgress;
			_antiSpillSceneName = antiSpillSceneName;
			_speedIntervals = speedIntervals;

			SceneManager.LoadScene(_loadingScreenSceneName, LoadSceneMode.Additive);
		}
		
		public static void SetHold(HoldModes holdMode, bool state)
		{
			if (_holds.ContainsKey(holdMode))
			{
				_holds[holdMode] = state;
			}
		}
		
		public static void ClearHolds()
		{
			foreach (KeyValuePair<HoldModes, bool> hold in _holds)
			{
				_holds[hold.Key] = false;
			}
		}
        
		private static Scene[] GetScenesToUnload(MMAdditiveSceneLoadingManagerSettings.UnloadMethods unloaded)
		{
	        
			switch (unloaded) {
				case MMAdditiveSceneLoadingManagerSettings.UnloadMethods.None:
					_initialScenes = new Scene[0];
					break;
				case MMAdditiveSceneLoadingManagerSettings.UnloadMethods.ActiveScene:
					_initialScenes = new Scene[1] {SceneManager.GetActiveScene()};
					break;
				default:
				case MMAdditiveSceneLoadingManagerSettings.UnloadMethods.AllScenes:
					_initialScenes = MMScene.GetLoadedScenes();
					break;
			}
			return _initialScenes;
		}


		/// <summary>
		/// Starts loading the new level asynchronously
		/// </summary>
		protected virtual void Awake()
		{
			Initialization();
		}

		/// <summary>
		/// Initializes timescale, computes null checks, and starts the load sequence
		/// </summary>
		protected virtual void Initialization()
		{
			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : Initialization");

			if (DebugMode)
			{
				foreach (Scene scene in _initialScenes)
				{
					MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : Initial scene : " + scene.name);
				}    
			}

			_setRealtimeProgressValueIsNull = SetRealtimeProgressValue == null;
			_setInterpolatedProgressValueIsNull = SetInterpolatedProgressValue == null;
			Time.timeScale = 1f;

			if ((_sceneToLoadName == "") || (_loadingScreenSceneName == ""))
			{
				return;
			}
            
			StartCoroutine(LoadSequence());
		}

		/// <summary>
		/// Every frame, we fill the bar smoothly according to loading progress
		/// </summary>
		protected virtual void Update()
		{
			UpdateProgress();
		}

		/// <summary>
		/// Sends progress value via UnityEvents
		/// </summary>
		protected virtual void UpdateProgress()
		{
			if (!_setRealtimeProgressValueIsNull)
			{
				SetRealtimeProgressValue.Invoke(_loadProgress);
			}

			if (_interpolateProgress)
			{
				_interpolatedLoadProgress = MMMaths.Approach(_interpolatedLoadProgress, _loadProgress, Time.unscaledDeltaTime * ComputeInterpolationSpeed(_interpolatedLoadProgress));
				if (!_setInterpolatedProgressValueIsNull)
				{
					SetInterpolatedProgressValue.Invoke(_interpolatedLoadProgress);	
				}
			}
			else
			{
				SetInterpolatedProgressValue.Invoke(_loadProgress);	
			}
		}

		/// <summary>
		/// Computes the interpolation speed to apply for a specific progress time 
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static float ComputeInterpolationSpeed(float t) 
		{
			if ((_speedIntervals != null) && (_speedIntervals.Count > 0))
			{
				foreach (MMSceneLoadingSpeedInterval interval in _speedIntervals)
				{
					if (interval.Interval.Contains(t))
					{
						return interval.Speed;
					}
				}
			}

			return _progressInterpolationSpeed;
		}

		/// <summary>
		/// Loads the scene to load asynchronously.
		/// </summary>
		protected virtual IEnumerator LoadSequence()
		{
			_antiSpill?.PrepareAntiFill(_sceneToLoadName, _antiSpillSceneName);
			InitiateLoad();
			yield return ProcessDelayBeforeEntryFade();
			yield return EntryFade();
			yield return ProcessDelayAfterEntryFade();
			yield return UnloadOriginScenes();
			yield return LoadDestinationScene();
			yield return ProcessDelayBeforeSceneActivation();
			yield return DestinationSceneActivation();
			yield return ProcessDelayAfterSceneActivation();
			yield return ExitFade();
			yield return UnloadSceneLoader();
		}

		/// <summary>
		/// Initializes counters and timescale
		/// </summary>
		protected virtual void InitiateLoad()
		{
			_loadProgress = 0f;
			_interpolatedLoadProgress = 0f;
			Time.timeScale = 1f;
			SetAudioListener(false);
			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : Initiate Load");
			MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.LoadStarted);
			OnLoadStarted?.Invoke();
		}

		/// <summary>
		/// Waits for the specified BeforeEntryFadeDelay duration
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ProcessDelayBeforeEntryFade()
		{
			if (_beforeEntryFadeDelay > 0f)
			{
				MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : delay before entry fade, duration : " + _beforeEntryFadeDelay);
				MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.BeforeEntryFade);
				OnBeforeEntryFade?.Invoke();
				
				yield return MMCoroutine.WaitForUnscaled(_beforeEntryFadeDelay);
			}
		}

		/// <summary>
		/// Calls a fader on entry
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator EntryFade()
		{
			if (_entryFadeDuration > 0f)
			{
				MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : starting entry fade, duration : " + _entryFadeDuration);
				MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.EntryFade);
				OnEntryFade?.Invoke();
				
				if (_fadeMode == FadeModes.FadeOutThenIn)
				{
					yield return null;
					MMFadeOutEvent.Trigger(_entryFadeDuration, _entryFadeTween, FaderID, true);
				}
				else
				{
					yield return null;
					MMFadeInEvent.Trigger(_entryFadeDuration, _entryFadeTween, FaderID, true);
				}           

				yield return MMCoroutine.WaitForUnscaled(_entryFadeDuration);
			}
		}

		/// <summary>
		/// Waits for the specified AfterEntryFadeDelay
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ProcessDelayAfterEntryFade()
		{
			while (_holds[HoldModes.AfterEntryFade])
			{
				yield return null;
			}
			if (_afterEntryFadeDelay > 0f)
			{
				MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : delay after entry fade, duration : " + _afterEntryFadeDelay);
				MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.AfterEntryFade);
				OnAfterEntryFade?.Invoke();
				yield return MMCoroutine.WaitForUnscaled(_afterEntryFadeDelay);
			}
		}

		/// <summary>
		/// Unloads the original scene(s) and waits for the unload to complete
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator UnloadOriginScenes()
		{
			foreach (Scene scene in _initialScenes)
			{
				MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : unload scene " + scene.name);
				MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.UnloadOriginScene);
				OnUnloadOriginScene?.Invoke();
				
				if (!scene.IsValid() || !scene.isLoaded)
				{
					Debug.LogWarning("MMLoadingSceneManagerAdditive : invalid scene : " + scene.name);
					continue;
				}
				
				_unloadOriginAsyncOperation = SceneManager.UnloadSceneAsync(scene);
				SetAudioListener(true);
				while (_unloadOriginAsyncOperation.progress < _asyncProgressLimit)
				{
					yield return null;
				}
			}
			
			while (_holds[HoldModes.AfterUnloadOriginScene])
			{
				yield return null;
			}
		}

		/// <summary>
		/// Loads the destination scene
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator LoadDestinationScene()
		{
			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : load destination scene");
			MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.LoadDestinationScene);
			OnLoadDestinationScene?.Invoke();

			_loadDestinationAsyncOperation = SceneManager.LoadSceneAsync(_sceneToLoadName, LoadSceneMode.Additive );
			_loadDestinationAsyncOperation.completed += OnLoadOperationComplete;

			_loadDestinationAsyncOperation.allowSceneActivation = false;
            
			while (_loadDestinationAsyncOperation.progress < _asyncProgressLimit)
			{
				_loadProgress = _loadDestinationAsyncOperation.progress;
				yield return null;
			}
            
			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : load progress complete");
			MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.LoadProgressComplete);
			OnLoadProgressComplete?.Invoke();

			// when the load is close to the end (it'll never reach it), we set it to 100%
			_loadProgress = 1f;

			// we wait for the bar to be visually filled to continue
			if (_interpolateProgress)
			{
				while (_interpolatedLoadProgress < 1f)
				{
					yield return null;
				}
			}			

			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : interpolated load complete");
			MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.InterpolatedLoadProgressComplete);
			OnInterpolatedLoadProgressComplete?.Invoke();
		}

		/// <summary>
		/// Waits for BeforeSceneActivation seconds
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ProcessDelayBeforeSceneActivation()
		{
			if (_beforeSceneActivationDelay > 0f)
			{
				MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : delay before scene activation, duration : " + _beforeSceneActivationDelay);
				MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.BeforeSceneActivation);
				OnBeforeSceneActivation?.Invoke();
				
				yield return MMCoroutine.WaitForUnscaled(_beforeSceneActivationDelay);
			}
			
			while (_holds[HoldModes.BeforeSceneActivation])
			{
				yield return null;
			}
		}

		/// <summary>
		/// Activates the destination scene
		/// </summary>
		protected virtual IEnumerator DestinationSceneActivation()
		{
			yield return MMCoroutine.WaitForFrames(1);
			_loadDestinationAsyncOperation.allowSceneActivation = true;
			while (_loadDestinationAsyncOperation.progress < 1.0f)
			{
				yield return null;
			}
			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : activating destination scene");
			MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.DestinationSceneActivation);
			OnDestinationSceneActivation?.Invoke();
		}

		protected virtual IEnumerator ProcessDelayAfterSceneActivation()
		{
			if (_afterSceneActivationDelay > 0f)
			{
				MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : delay after scene activation, duration : " + _afterSceneActivationDelay);
				MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.AfterSceneActivation);
				OnAfterSceneActivation?.Invoke();
				
				yield return MMCoroutine.WaitForUnscaled(_afterSceneActivationDelay);
			}
		}

		/// <summary>
		/// Requests a fade on exit
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ExitFade()
		{
			while (_holds[HoldModes.BeforeExitFade])
			{
				yield return null;
			}
			SetAudioListener(false);
			if (_exitFadeDuration > 0f)
			{
				MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : starting exit fade, duration : " + _exitFadeDuration);
				MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.ExitFade);
				OnExitFade?.Invoke();
				
				if (_fadeMode == FadeModes.FadeOutThenIn)
				{
					MMFadeInEvent.Trigger(_exitFadeDuration, _exitFadeTween, FaderID, true);
				}
				else
				{
					MMFadeOutEvent.Trigger(_exitFadeDuration, _exitFadeTween, FaderID, true);
				}
				yield return MMCoroutine.WaitForUnscaled(_exitFadeDuration);
			}
		}

		/// <summary>
		/// A method triggered when the async operation completes
		/// </summary>
		/// <param name="obj"></param>
		protected virtual void OnLoadOperationComplete(AsyncOperation obj)
		{
			SceneManager.SetActiveScene(SceneManager.GetSceneByName(_sceneToLoadName));
			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : set active scene to " + _sceneToLoadName);

		}

		/// <summary>
		/// Unloads the scene loader
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator UnloadSceneLoader()
		{
			MMLoadingSceneDebug("MMLoadingSceneManagerAdditive : unloading scene loader");
			MMSceneLoadingManager.LoadingSceneEvent.Trigger(_sceneToLoadName, MMSceneLoadingManager.LoadingStatus.UnloadSceneLoader);
			OnUnloadSceneLoader?.Invoke();
			
			yield return null; // mandatory yield to avoid an unjustified warning
			_unloadLoadingAsyncOperation = SceneManager.UnloadSceneAsync(_loadingScreenSceneName);
			while (_unloadLoadingAsyncOperation.progress < _asyncProgressLimit)
			{
				yield return null;
			}
		}

		/// <summary>
		/// Turns the loading audio listener on or off
		/// </summary>
		/// <param name="state"></param>
		protected virtual void SetAudioListener(bool state)
		{
			if (LoadingAudioListener != null)
			{
				//LoadingAudioListener.gameObject.SetActive(state);
			}
		}

		/// <summary>
		/// On Destroy we reset our state
		/// </summary>
		protected virtual void OnDestroy()
		{
			_loadingInProgress = false;
		}

		/// <summary>
		/// A debug method used to output console messages, for this class only
		/// </summary>
		/// <param name="message"></param>
		protected virtual void MMLoadingSceneDebug(string message)
		{
			if (!DebugMode)
			{
				return;
			}
			
			string output = "";
			output += "<color=#82d3f9>[" + Time.frameCount + "]</color> ";
			output += "<color=#f9a682>[" + MMTime.FloatToTimeString(Time.time, false, true, true, true) + "]</color> ";
			output +=  message;
			MMDebug.DebugLogInfo(output);
		}
	}
}