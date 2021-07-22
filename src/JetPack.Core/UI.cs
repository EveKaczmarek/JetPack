using UnityEngine;

namespace JetPack
{
	public partial class UI
	{
		public static void ShiftX(GameObject _obj, float _amount) => Resize(_obj, _amount, ResizeMode.ShiftX);
		public static void ShiftY(GameObject _obj, float _amount) => Resize(_obj, _amount, ResizeMode.ShiftY);

		public static void Shift(GameObject _obj, float _amountX, float _amountY)
		{
			RectTransform _rt = _obj.GetComponent<RectTransform>();
			_rt.offsetMin = new Vector2(_rt.offsetMin.x + _amountX, _rt.offsetMin.y + _amountY);
			_rt.offsetMax = new Vector2(_rt.offsetMax.x + _amountX, _rt.offsetMax.y + _amountY);
		}

		public static void Resize(GameObject _obj, float _amount, ResizeMode _mode)
		{
			RectTransform _rt = _obj.GetComponent<RectTransform>();
			if ((_mode == ResizeMode.MinX) || (_mode == ResizeMode.ShiftX))
				_rt.offsetMin = new Vector2(_rt.offsetMin.x + _amount, _rt.offsetMin.y);
			if ((_mode == ResizeMode.MaxX) || (_mode == ResizeMode.ShiftX))
				_rt.offsetMax = new Vector2(_rt.offsetMax.x + _amount, _rt.offsetMax.y);
			if ((_mode == ResizeMode.MinY) || (_mode == ResizeMode.ShiftY))
				_rt.offsetMin = new Vector2(_rt.offsetMin.x, _rt.offsetMin.y + _amount);
			if ((_mode == ResizeMode.MaxY) || (_mode == ResizeMode.ShiftY))
				_rt.offsetMax = new Vector2(_rt.offsetMax.x, _rt.offsetMax.y + _amount);
		}

		public enum ResizeMode { MinX, MaxX, MinY, MaxY, ShiftX, ShiftY }

		public static Texture2D MakePlainTex(int _width, int _height, Color _color)
		{
			Color[] _pix = new Color[_width * _height];

			for (int i = 0; i < _pix.Length; i++)
				_pix[i] = _color;

			Texture2D _result = new Texture2D(_width, _height);
			_result.SetPixels(_pix);
			_result.Apply();

			return _result;
		}

		// https://bensilvis.com/unity3d-auto-scale-gui/
		public static Rect GetResizedRect(Rect _rect)
		{
			Vector2 _position = GUI.matrix.MultiplyVector(new Vector2(_rect.x, _rect.y));
			Vector2 _size = GUI.matrix.MultiplyVector(new Vector2(_rect.width, _rect.height));

			return new Rect(_position.x, _position.y, _size.x, _size.y);
		}
	}
}
