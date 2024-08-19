using Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore;
using IDestroyable = Interfaces.IDestroyable;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider))]
public class DestroyableSystem : MonoBehaviour, IDestroyable
{
    public static Action<Debree> DebreeAttaching { get; set; }

    float _probabilityOfDebreeAttach = 0.3f;
    float _probabilityPartialDesctruction = 0.1f;

    [SerializeField] Collider _initialCollisionCollider;
    [SerializeField] int _levelOfTheCarNeededForDestroyment = 1;
    [SerializeField] ParticleSystem _destroyParticles;
    [SerializeField] DestructionType _desctructionType;
    [SerializeField] float _slowCarVelocityMultiplier = 0.9f;
    [SerializeField] int _destructionForceMultiplier = 50;
    [SerializeField] List<MeshRenderer> _initialMeshRenderers;
    [SerializeField] List<Debree> _fragments;

    int _howManyPartialDestructionUntilTheFullOne = 2;
    int _numberOfPartialDestructions = 0;
    int _ignoreCollisionsByLevelDifference = 2;
    public event IDestroyable.DestroyableDelegate DestructionEvent;

    List<Collider> _playerColliderTouching = new List<Collider>();
    bool _isDestroying = false;
    private void Awake()
    {
        _fragments.ForEach(x => x.DebreeDeleteMessage += OnDebreeDeleted);
    }
    public enum DestructionType
    {
        Building, Prop, TrafficCar, CopCar
    }

    private void OnTriggerEnter(Collider other)
    {   
        if (other.CompareTag("Player") && _playerColliderTouching.Count==0)
        {
            _playerColliderTouching.Add(other);

            if ((GameManager.Instance.CurrentPlayerLevel >= _levelOfTheCarNeededForDestroyment) || (_numberOfPartialDestructions>=_howManyPartialDestructionUntilTheFullOne))
            {
                if ((GameManager.Instance.CurrentPlayerLevel - _levelOfTheCarNeededForDestroyment) <= _ignoreCollisionsByLevelDifference)
                {
                    if(!_isDestroying)
                        DestroyTheObject();
                }
                else
                {
                    _initialCollisionCollider.enabled = false;
                    Destroy(gameObject);
                }
            }
            else if ((_levelOfTheCarNeededForDestroyment- GameManager.Instance.CurrentPlayerLevel) == 1)        //Make partial destruction on object only one level above (so tiny car doesnt destroy scyscraper)

            {
                PartiallyDestroyTheObject();
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        _playerColliderTouching.Remove(other);
    }

    void PartiallyDestroyTheObject()
    {
        _initialMeshRenderers.ForEach(x=>x.enabled = false);
        _initialCollisionCollider.enabled = false;

        for (int i = 0; i < _fragments.Count; i++)
        {
            _fragments[i].gameObject.SetActive(true);
            if (Random.Range(0, 1.0f) <= _probabilityPartialDesctruction)
            {
                _fragments[i].AddExplosionForce( transform.position, _destructionForceMultiplier);
            }

        }
        _numberOfPartialDestructions++;
    }
    void DestroyTheObject()
    {
        _isDestroying = true;
        _initialMeshRenderers.ForEach(x => x.enabled = false);
        _initialCollisionCollider.enabled = false;

        for(int i=0;i<_fragments.Count;i++)
        {
            _fragments[i].gameObject.SetActive(true);
            _fragments[i].AddExplosionForce(transform.position, _destructionForceMultiplier);
            if (Random.Range(0, 1.0f)<=_probabilityOfDebreeAttach)
            {
                DebreeAttaching?.Invoke(_fragments[i]);
            }
        }
        if (_destroyParticles != null)
        {
            _destroyParticles.gameObject?.SetActive(true);
            StartCoroutine(TurnOffParticles());
        }
        DestructionEvent?.Invoke(gameObject,EventArgs.Empty);


        IEnumerator TurnOffParticles()
        {
            yield return new WaitForSeconds(_destroyParticles.main.duration);
            _destroyParticles.gameObject?.SetActive(false);
        }
    }
    void OnDebreeDeleted(Debree debree)
    {
        if (debree != null)
        {
            _fragments.Remove(debree);
            Destroy(debree.gameObject);

            if (_fragments.Count == 0)
                Destroy(gameObject);
        }
    }
}