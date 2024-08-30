
using ImGuiNET;

namespace Graybox.Editor.Widgets;

internal class ProfilerWidget : BaseWidget
{

	public override string Title => "Profiler";

	internal static ProfilerWidget Instance;

	Dictionary<string, Profiler.SampleData> samples;

	public ProfilerWidget()
	{
		Instance = this;
		Debug.Profile = Display;
	}

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );

		if ( samples == null )
		{
			ImGui.Text( "No samples available." );
			return;
		}

		if ( ImGui.BeginTable( "Performance Table", 3, ImGuiTableFlags.Borders ) )
		{
			ImGui.TableSetupColumn( "Sample" );
			ImGui.TableSetupColumn( "Duration (ms)" );
			ImGui.TableSetupColumn( "Memory (MB)" );
			ImGui.TableHeadersRow();

			foreach ( var sample in samples )
			{
				ImGui.TableNextRow( ImGuiTableRowFlags.None );

				if ( ImGui.IsItemHovered() )
				{
					var hoverColor = new SVector4( 0.3f, 0.3f, 0.3f, 0.1f );
					//ImGui.TableSetBgColor( ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32( hoverColor ) );
				}

				ImGui.TableSetColumnIndex( 0 );
				ImGui.Text( sample.Key );

				ImGui.TableSetColumnIndex( 1 );
				ImGui.Text( $"{sample.Value.Duration} ms" );

				ImGui.TableSetColumnIndex( 2 );
				double memoryInMB = sample.Value.MemoryUsed / 1024.0 / 1024.0;
				var color = memoryInMB > 10 ? new SVector4( 1, 0.3f, 0.3f, 1 ) : new SVector4( 0, 0.8f, 0, 1 );
				ImGui.TextColored( color, $"{memoryInMB:F2} MB" );
			}

			ImGui.EndTable();
		}
	}

	public void Display( Profiler profiler )
	{
		samples = profiler?.GetMergedSamples();
	}

}
