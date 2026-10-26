using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    public event Action<bool> OnValueChanged;

    [SerializeField] private Image _image;
    [SerializeField] private Sprite _onSprite;
    [SerializeField] private Sprite _offSprite;

    private bool _isOn = true;
    private Vector3 _baseScale = Vector3.one;
    private Coroutine _animateCoroutine;

    public bool IsOn
    {
        get => _isOn;
        set => SetState(value, false);
    }

    private void Awake()
    {
        EnsureInitialized();
        UpdateVisualsInstant();
    }

    private void OnEnable()
    {
        UpdateVisualsInstant();
    }

    private void EnsureInitialized()
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }

        if (_image != null && !_image.preserveAspect)
        {
            _image.preserveAspect = true;
        }

        if (_onSprite == null)
        {
            _onSprite = UIAssets.GetSprite("Toggle_On");
        }

        if (_offSprite == null)
        {
            _offSprite = UIAssets.GetSprite("Toggle_Off");
        }

        if (_baseScale == Vector3.one && transform != null)
        {
            _baseScale = transform.localScale;
        }

    }

    private void OnDisable()
    {
        if (_animateCoroutine != null)
        {
            StopCoroutine(_animateCoroutine);
            _animateCoroutine = null;
        }
        transform.localScale = _baseScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SetState(!_isOn, animate: true);
        OnValueChanged?.Invoke(_isOn);
    }

    public void SetState(bool state, bool animate = true)
    {
        _isOn = state;
        EnsureInitialized();

        if (animate && gameObject.activeInHierarchy)
        {
            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
            }


            _animateCoroutine = StartCoroutine(AnimateTransition());
        }
        else
        {
            UpdateVisualsInstant();
        }
    }

    private void UpdateVisualsInstant()
    {
        EnsureInitialized();
        if (_image == null)
        {
            return;
        }


        if (_onSprite != null && _offSprite != null)
        {
            _image.sprite = _isOn ? _onSprite : _offSprite;
            _image.color = Color.white;
        }
        else
        {
            _image.color = _isOn ? UIAssets.ToggleGreen : UIAssets.ToggleGray;
        }
    }

    private IEnumerator AnimateTransition()
    {
        UpdateVisualsInstant();
        yield return UIAnimationHelper.PunchScale(transform, _baseScale, 0.08f, 0.16f);
        _animateCoroutine = null;
    }
}
