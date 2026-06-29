using Game.Utils;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Game.UGCEditor
{
	public class UGCRemoteLoader : MonoBehaviour
	{
		[SerializeField]protected RawImage _RawImage = null;
		[SerializeField] public int tryCount = 3;
		private string _CurrentRequestedURL;
		private bool _DestroyPending;
		private Texture2D _Texture;
		private IPool _Pool;
		
		public RawImage RawImage => _RawImage;


		public void InitializeWithPool(IPool pool)
		{
			_Pool = pool;
		}

		void Awake()
		{
			if (!_RawImage)
				_RawImage = GetComponent<RawImage>();
		}
		

		public void Release(string imageURL)
		{
			if (_Pool != null)
			{
				_Pool.Relase(imageURL);
			}
		}

		public void Load(string imageURL, Action<Texture> onCompleted = null,Action onFailed = null)
		{
			bool currentRequestedURLAlreadyLoaded = _CurrentRequestedURL == imageURL;
			_CurrentRequestedURL = imageURL;
			bool foundCached = false;
			if (currentRequestedURLAlreadyLoaded)
			{
				onCompleted?.Invoke(_Texture);
				return;
			}

			if (_Pool != null)
			{
				Texture2D cachedInPool = _Pool.Get(imageURL) as Texture2D;
				if (cachedInPool)
				{
					_Texture = cachedInPool;
					foundCached = true;
				}
			}

			if (foundCached)
			{
				if (_RawImage != null)
				{
					_RawImage.texture = _Texture;
					_RawImage.color = Color.white;
				}

				onCompleted?.Invoke(_Texture);
				return;
			}

			var request = new GameSimpleImageDownloader.Request()
			{
				url = imageURL,
				onDone = result =>
				{
					if (imageURL != _CurrentRequestedURL || _DestroyPending)
					{
						var tex = result.CreateTextureFromReceivedData();
						DisposeTexture(tex);
						return;
					}

					if (!_DestroyPending &&
					    imageURL ==
					    _CurrentRequestedURL) // this will be false if a new request was done during downloading, case in which the result will be ignored
					{
						if (_Pool == null)
						{
							if (_Texture)
								DisposeTexture(_Texture);
							_Texture = result.CreateTextureFromReceivedData();
						}
						else
						{
							var textureAlreadyStoredMeanwhile = _Pool.Get(imageURL);
							bool someoneStoredTheImageSooner = textureAlreadyStoredMeanwhile != null;
							if (someoneStoredTheImageSooner)
							{
								var tex = result.CreateTextureFromReceivedData();
								DisposeTexture(tex);
								_Texture = textureAlreadyStoredMeanwhile as Texture2D;
							}
							else
							{
								_Texture = result.CreateTextureFromReceivedData();
								_Pool.Put(imageURL, _Texture);
							}
						}

						if (_RawImage != null)
						{
							_RawImage.color = Color.white;
							_RawImage.texture = _Texture;
						}

						onCompleted?.Invoke(_Texture);
					}
				},
				onError = () =>
				{
					if (!_DestroyPending &&
					    imageURL ==
					    _CurrentRequestedURL) // this will be false if a new request was done during downloading, case in which the result will be ignored
					{
						tryCount--;
						if (tryCount > 0)
						{
							Load(_CurrentRequestedURL,onCompleted,onFailed);
						}
						else
						{
							onFailed?.Invoke();
						}
					}
				}
			};
			GameSimpleImageDownloader.Instance.Enqueue(request);
		}

		void OnDestroy()
		{
			_DestroyPending = true;

			if (_Pool == null && _Texture)
			{
				DisposeTexture(_Texture);
			}
		}

		private static void DisposeTexture(Texture2D tex)
		{
			try
			{
				UnityEngine.Object.Destroy(tex as UnityEngine.Object);
			}
			catch
			{
			}
		}
	}
}