using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal abstract class CombatPredictionInteractionSession : IDisposable
{
	private CombatPredictionTargetObserver? _targetObserver;

	private bool _disposed;

	public abstract AbstractModel Source { get; }

	public CombatPredictionInteractionMode Mode { get; }

	public bool IsTargeting => _targetObserver != null;

	public Creature? Target { get; private set; }

	public CombatPredictionProjection? Projection { get; private set; }

	public event Action? ProjectionChanged;

	public event Action? TargetingFinishing;

	protected CombatPredictionInteractionSession(CombatPredictionInteractionMode mode)
	{
		Mode = mode;
		base._002Ector();
	}

	public void RefreshUntargeted()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (IsTargeting)
		{
			throw new InvalidOperationException("A targeting session cannot use automatic target resolution.");
		}
		StopObservingTargets();
		Target = null;
		RefreshProjection();
	}

	public void BeginTargeting(NTargetManager targetManager)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (Mode != CombatPredictionInteractionMode.Action || IsTargeting)
		{
			throw new InvalidOperationException("Only a non-targeting action session can begin targeting.");
		}
		Target = null;
		SetProjection(null);
		_targetObserver = new CombatPredictionTargetObserver(targetManager);
		_targetObserver.TargetChanged += OnTargetChanged;
		_targetObserver.TargetingFinishing += OnTargetingFinishing;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			StopObservingTargets();
			CombatPredictionProjectionController.Release(this);
			this.ProjectionChanged = null;
		}
	}

	protected abstract CombatPredictionProjection? Predict(Creature? target);

	private void OnTargetChanged(Creature? target)
	{
		Target = target;
		if (target == null)
		{
			SetProjection(null);
		}
		else
		{
			RefreshProjection();
		}
	}

	private void OnTargetingFinishing()
	{
		StopObservingTargets();
		this.TargetingFinishing?.Invoke();
	}

	private void RefreshProjection()
	{
		CombatPredictionProjection projection;
		try
		{
			projection = Predict(Target);
		}
		catch (Exception ex)
		{
			Logger logger = Entry.Logger;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Combat prediction failed for ");
			defaultInterpolatedStringHandler.AppendFormatted<ModelId>(Source.Id);
			defaultInterpolatedStringHandler.AppendLiteral(" targeting ");
			Creature? target = Target;
			defaultInterpolatedStringHandler.AppendFormatted((target != null) ? target.Name : null);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(ex);
			logger.Warn(defaultInterpolatedStringHandler.ToStringAndClear(), 1);
			ModTelemetry.CaptureException(ex, "combat_prediction_session", "refresh_projection", new
			{
				Source = TelemetryContext.ForModel(Source),
				Target = ((Target == null) ? null : TelemetryContext.ForCreature(Target))
			});
			projection = null;
		}
		SetProjection(projection);
	}

	private void SetProjection(CombatPredictionProjection? projection)
	{
		Projection = projection;
		CombatPredictionProjectionController.Set(this, projection);
		this.ProjectionChanged?.Invoke();
	}

	private void StopObservingTargets()
	{
		if (_targetObserver != null)
		{
			_targetObserver.Dispose();
			_targetObserver = null;
		}
	}
}
