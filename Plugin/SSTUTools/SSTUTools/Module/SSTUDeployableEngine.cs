﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SSTUTools
{
    public class SSTUDeployableEngine : PartModule
    {
        /// <summary>
        /// engine ID for the engine module that this deployable engine module is responsible for
        /// </summary>
        [KSPField]
        public string engineID = "Engine";

        [KSPField(isPersistant = true)]
        public String persistentState = AnimState.STOPPED_START.ToString();

        [Persistent]
        public string configNodeData = string.Empty;

        private bool initialized = false;
                
        private AnimationModule<SSTUDeployableEngine> animationModule;

        private ModuleEnginesFX engineModule;
        
        [KSPAction("Activate Engine")]
        public void deployEngineAction(KSPActionParam param)
        {
            deployEngineEvent();
        }

        [KSPAction("Shutdown Engine")]
        public void retractEngineAction(KSPActionParam param)
        {
            retractEngineEvent();
        }

        [KSPEvent(name = "deployEngineEvent", guiName = "Activate Engine", guiActive = true, guiActiveEditor = false)]
        public void deployEngineEvent()
        {
            animationModule.onDeployEvent();
        }

        [KSPEvent(name = "retractEngineEvent", guiName = "Shutdown Engine", guiActive = true, guiActiveEditor = false)]
        public void retractEngineEvent()
        {
            animationModule.onRetractEvent();
        }

        public void Start()
        {
            engineModule = null;
            ModuleEnginesFX[] engines = part.GetComponents<ModuleEnginesFX>();
            int len = engines.Length;
            for (int i = 0; i < len; i++)
            {
                if (engines[i].engineID == engineID)
                {
                    engineModule = engines[i];
                }
            }
            if (engineModule == null)
            {
                MonoBehaviour.print("ERROR: Could not locate engine by ID: " + engineID + " for part: " + part + " for SSTUDeployableEngine.  This will cause errors during gameplay.  Setting engine to first engine module (if present)");                
            }
            setupEngineModuleGui();
        }

        public void onAnimationStateChange(AnimState newState)
        {
            if (newState == AnimState.STOPPED_END && HighLogic.LoadedSceneIsFlight)
            {
                engineModule.Activate();
            }
        }
        
        public override void OnActive()
        {
            if (animationModule.animState == AnimState.STOPPED_END)
            {
                engineModule.Activate();
            }
            else
            {
                deployEngineEvent();
                if (engineModule.EngineIgnited)
                {
                    engineModule.Shutdown();
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (string.IsNullOrEmpty(configNodeData)) { configNodeData = node.ToString(); }
            initialize();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            initialize();
        }

        public void reInitialize()
        {
            initialized = false;
            animationModule = null;
            initialize();
        }

        private void initialize()
        {
            if (initialized) { return; }
            initialized = true;
            ConfigNode node = SSTUConfigNodeUtils.parseConfigNode(configNodeData);
            AnimationData animData = new AnimationData(node.GetNode("ANIMATIONDATA"));
            animationModule = new AnimationModule<SSTUDeployableEngine>(part, this, Fields[nameof(persistentState)], null, Events[nameof(deployEngineEvent)], Events[nameof(retractEngineEvent)]);
            animationModule.getSymmetryModule = m => m.animationModule;
            animationModule.setupAnimations(animData, part.transform.FindRecursive("model"), 0);
            animationModule.onAnimStateChangeCallback = onAnimationStateChange;
        }

        private void setAnimationState(AnimState state)
        {
            AnimState currentState = animationModule.animState;
            if (state == AnimState.STOPPED_END)
            {
                engineModule.OnActive();
            }
        }

        private void setupEngineModuleGui()
        {
            engineModule.Events[nameof(engineModule.Activate)].active = false;
            engineModule.Events[nameof(engineModule.Shutdown)].active = false;
            engineModule.Events[nameof(engineModule.Activate)].guiActive = false;
            engineModule.Events[nameof(engineModule.Shutdown)].guiActive = false;
            engineModule.Actions[nameof(engineModule.ActivateAction)].active = false;
            engineModule.Actions[nameof(engineModule.ShutdownAction)].active = false;
            engineModule.Actions[nameof(engineModule.OnAction)].active = false;
        }

    }
}
