using System;
using System.Collections;
using System.Collections.Generic;
using OneBitLab.FluidSim;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA;

public class DropsPerSecSlider : MonoBehaviour
{
    //-------------------------------------------------------------
    [SerializeField]
    private Text m_DropsAmountText;

    private WaveSpawnSystem m_WaveSpawnSystem;
    
    //-------------------------------------------------------------
    private void Start()
    {
        m_WaveSpawnSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<WaveSpawnSystem>();
    }

    //-------------------------------------------------------------
    public void UpdateParticleValue( float amount )
    {
        m_DropsAmountText.text = ( (int)amount ).ToString();
        m_WaveSpawnSystem.DropsPerSecond = amount;
    }
    //-------------------------------------------------------------
}
