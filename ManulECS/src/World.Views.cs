using System.Collections.Generic;

namespace ManulECS {
  public partial class World {
    internal readonly Dictionary<FlagEnum, View> views = new();

    internal View GetView(World world, FlagEnum matcher) {
      if (views.TryGetValue(matcher, out View existingView)) {
        existingView.Update(world, matcher);
        return existingView;
      } else {
        var view = new View(world, matcher);
        views.Add(matcher, view);
        return view;
      }
    }

    public View View<T1>() where T1 : struct, IBaseComponent =>
      GetView(this, new FlagEnum(GetFlag<T1>()));

    public View View<T1, T2>()
      where T1 : struct, IBaseComponent
      where T2 : struct, IBaseComponent =>
        GetView(this, new FlagEnum(GetFlag<T1>(), GetFlag<T2>()));

    public View View<T1, T2, T3>()
      where T1 : struct, IBaseComponent
      where T2 : struct, IBaseComponent
      where T3 : struct, IBaseComponent =>
        GetView(this, new FlagEnum(GetFlag<T1>(), GetFlag<T2>(), GetFlag<T3>()));

    public View View<T1, T2, T3, T4>()
      where T1 : struct, IBaseComponent
      where T2 : struct, IBaseComponent
      where T3 : struct, IBaseComponent
      where T4 : struct, IBaseComponent =>
        GetView(this, new FlagEnum(GetFlag<T1>(), GetFlag<T2>(), GetFlag<T3>(), GetFlag<T4>()));

    public View View<T1, T2, T3, T4, T5>()
      where T1 : struct, IBaseComponent
      where T2 : struct, IBaseComponent
      where T3 : struct, IBaseComponent
      where T4 : struct, IBaseComponent
      where T5 : struct, IBaseComponent =>
        GetView(this, new FlagEnum(GetFlag<T1>(), GetFlag<T2>(), GetFlag<T3>(), GetFlag<T4>(), GetFlag<T5>()));
  }
}
