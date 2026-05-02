return await Hj.SourceMix.SourceMixCommandFactory
  .Create()
  .Parse(args)
  .InvokeAsync()
  .ConfigureAwait(false);
