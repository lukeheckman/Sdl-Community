﻿using Sdl.Community.RateItControl.API;

namespace Sdl.Community.RateItControl.Implementation
{
	public class RageItItem: BaseModel, IRateItItem
	{
		private bool _selected;

		public bool Selected
		{
			get => _selected;
			set
			{
				_selected = value;
				OnPropertyChanged(nameof(Selected));
			}
		}
	}
}