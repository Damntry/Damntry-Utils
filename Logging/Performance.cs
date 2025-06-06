﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Damntry.Utils.ExtensionMethods;
using Damntry.Utils.Tasks;
using Damntry.Utils.Tasks.AsyncDelay;


namespace Damntry.Utils.Logging {

	//TODO Global 5 - The first measure should be ignored on methods that are called a few times. I should probably show it in
	//	its own column as extra data since its not representative of normal usage for benchmark purposes, unless its something that is
	//	called once or twice. In any case its still important to know.
	//TODO Global 6 - I should probably move this into a Singleton and add proper thread safety, but then I would need to start doing
	//	"Import static ..." on every class, or call its methods the longer way (Performance.XXXX.Start...).
	//	Remember that vars setting their values at the same time than declaring, now need to set their values at the constructor.

	//TODO Global 4 - Its getting cramped. Make a new Performance subfolder and move all classes to their own files inside of it.
	/*TODO Global 4 - Tarkov uses an interesting method to measure with using, like this: 
		using (GClass21.StartWithToken("LoadLocation")) {
			measured code.
		}

		Looks neater so add this as a new way of doing it.
		Keep the old method because its necessary for measures that span multiple methods.
	*/

	/// <summary>
	/// Performance measuring class. Logs both to a TimeLogger, and, if enabled, its own log file.
	/// Times are saved and calculated for each measure with the same name, across multiple runs, to provide performance data.
	/// </summary>
	public static class Performance {

		private static Dictionary<string, StopwatchMeasure> mapMeasures = new Dictionary<string, StopwatchMeasure>();

		/// <summary>Automatically calculated length of the measure with the longest name.</summary>
		private static int measureNameMaxLength;

		/// <summary>Number of decimals to show on measure timings</summary>
		private static readonly int MeasureDecimals = 3;

		/// <summary>The fixed number of caracters for the measured timings in the performance table.</summary>
		private static readonly int MeasureTimingsPadding = MeasureDecimals + 9;

		/// <summary>
		/// The fixed number of caracters for the column with the total measured 
		/// time in the performance table. It can get too big otherwise.
		/// </summary>
		private static readonly int MeasureTotalTimingPadding = MeasureTimingsPadding + 2;

		/// <summary>The fixed number of caracters for the column with the run count in the performance table.</summary>
		private static readonly int RunCountPadding = 8;

		/// <summary>
		/// Small utility stopwatch used to show in the performance table
		/// the total time since the first performance measure started running.
		/// </summary>
		private static Stopwatch swPerfTotalRunTime;

		/// <summary>Non dynamic part of the performance table header.</summary>
		private static readonly Lazy<string> StaticHeaderText = new Lazy<string>(() => GetStaticHeaderText());

		/// <summary>Non dynamic part of the performance table horizontal separator.</summary>
		private static readonly Lazy<string> StaticHorizontalSeparatorText = new Lazy<string>(() => GetStaticHorizontalSeparatorText());

		/// <summary>Action that writes on our own separate log file</summary>
		private static Action<string> logPerfAction = (string text) => PerfLogger.DLog.LogPerformance(text);

		/// <summary>Task for the periodic logging of performance table totals.</summary>
		private static CancellableSingleTask<AsyncDelay> logTableTask = new CancellableSingleTask<AsyncDelay>();


		private enum StopResetAction {
			Stop,
			Reset,
			Record,
			Log
		}


		public static void Start(string measureName, bool logTotals = true) {
			StopwatchMeasure swMeasure = GetCreateMeasure(measureName, logTotals);
			swMeasure.Start(false);
		}

		/// <summary>Deletes all previous run values, and starts performance timing from zero.</summary>
		public static void StartOver(string measureName, bool logTotals = true) {
			StopwatchMeasure swMeasure = GetCreateMeasure(measureName, logTotals);
			swMeasure.Start(true);
		}

		/// <summary>
		/// Resets performance timing, losing the currently ongoing timer value, if running.
		/// </summary>
		public static void Reset(string measureName, bool doNotWarn = false) {
			StopOrReset(measureName, StopResetAction.Reset, doNotWarn);
		}

		/// <summary>
		/// Stops performance timing. Can be resumed with Start(...).
		/// </summary>
		public static void Stop(string measureName, bool doNotWarn = false) {
			StopOrReset(measureName, StopResetAction.Stop, doNotWarn);
		}

		/// <summary>
		/// Stops performance timing, and if it was running, records current run, logs it, and resets to be ready for a new run.
		/// </summary>
		public static void StopAndLog(string measureName, bool doNotWarn = false) {
			StopOrReset(measureName, StopResetAction.Log, doNotWarn);
		}

		/// <summary>
		/// Stops performance timing, and if it was running, records current run without logging it, and resets to be ready for a new run.
		/// </summary>
		public static void StopAndRecord(string measureName, bool doNotWarn = false) {
			StopOrReset(measureName, StopResetAction.Record, doNotWarn);
		}

		private static void StopOrReset(string measureName, StopResetAction stopResetAction, bool doNotWarn) {
			StopwatchMeasure swMeasure = GetMeasure(measureName, doNotWarn);
			if (swMeasure == null) {
				return;
			}
			if (stopResetAction == StopResetAction.Reset) {
				swMeasure.Reset();
				
			} else {
				if (swMeasure.IsRunning) {
					swMeasure.Stop();
				}

				if (stopResetAction == StopResetAction.Record || stopResetAction == StopResetAction.Log) {
					swMeasure.RecordRun();

					if (stopResetAction == StopResetAction.Log) {
						LogPerformance(() => swMeasure.GetLogString());
					}
				}
			}
		}

		private static StopwatchMeasure GetCreateMeasure(string measureName, bool logTotals) {
			StopwatchMeasure swMeasure;
			mapMeasures.TryGetValue(measureName, out swMeasure);
			if (swMeasure == null) {
				swMeasure = new StopwatchMeasure(measureName, logTotals);
				mapMeasures.Add(measureName, swMeasure);

				measureNameMaxLength = Math.Max(measureNameMaxLength, measureName.Length);
				if (swPerfTotalRunTime == null) {
					swPerfTotalRunTime = Stopwatch.StartNew();
				}
			}

			return swMeasure;
		}

		private static StopwatchMeasure GetMeasure(string measureName, bool doNotWarn) {
			bool exists = mapMeasures.TryGetValue(measureName, out StopwatchMeasure stopwatchMeasure);
			if (!exists && !doNotWarn) {
				TimeLogger.Logger.LogTimeWarning($"The specified stopwatch \"{measureName}\" doesnt exist.", 
					LogCategories.PerfTest);
			}

			return stopwatchMeasure;
		}

		private static async Task LogPerformanceTableInterval(int logTableIntervalMillis) {
			while (!logTableTask.IsCancellationRequested) {
				await Task.Delay(logTableIntervalMillis, logTableTask.CancellationToken);

				Performance.LogPerformanceTable();
			}
		}

		private static void LogPerformanceTable() {
			LogPerformance(() => GetAllMeasuresTotalsSorted());
		}

		private static void LogPerformance(Func<string> logTextFunc) {
			TimeLogger.Logger.LogTimeDebugFunc(logTextFunc, LogCategories.PerfTest, (logPerfAction, true));
		}

		private static string GetAllMeasuresTotalsSorted() {
			//Sort by total time spent, from higher to lower
			List<StopwatchMeasure> listMeasuresSorted = mapMeasures.Values.ToList<StopwatchMeasure>().
				OrderByDescending(x => x.TotalMilli).
				ToList<StopwatchMeasure>();
			string tabPaddings = "".PadRight(5, '\t');

			string horizontalSeparator = GetLogStringSummaryHorizontalSeparator();
			string headerText = GetLogStringSummaryHeader();

			StringBuilder measureTotalsTable = new StringBuilder(100 + (125 * listMeasuresSorted.Count));

			measureTotalsTable.Append("Performance table, sorted by total time. ");
			if (swPerfTotalRunTime != null) {
				measureTotalsTable.AppendLine($"The total run time since the Start of the first measure is " +
					$"{swPerfTotalRunTime.Elapsed.ToString("hh':'mm':'ss'.'fff")}");
			} else {
				measureTotalsTable.AppendLine($"No measures have been created yet.");
			}

			//Performance table header
			measureTotalsTable.Append(tabPaddings);
			measureTotalsTable.AppendLine(horizontalSeparator);

			measureTotalsTable.Append(tabPaddings);
			measureTotalsTable.AppendLine(headerText);

			measureTotalsTable.Append(tabPaddings);
			measureTotalsTable.Append(horizontalSeparator);

			//Performance table timings
			foreach (StopwatchMeasure measure in listMeasuresSorted) {
				measureTotalsTable.AppendLine();
				measureTotalsTable.Append(tabPaddings);
				measureTotalsTable.Append(GetLogStringSummaryRunValues(measure));
			}

			measureTotalsTable.AppendLine();
			measureTotalsTable.Append(tabPaddings);
			measureTotalsTable.AppendLine(horizontalSeparator);

			return measureTotalsTable.ToString();
		}

		private static string GetLogStringSummaryHeader() {
			return $"| {"Measure name".PadSides(measureNameMaxLength)} {StaticHeaderText.Value}";
		}

		private static string GetLogStringSummaryHorizontalSeparator() {
			return $"|-{"".PadSides(measureNameMaxLength, '-')}-{StaticHorizontalSeparatorText.Value}";
		}

		private static string GetLogStringSummaryRunValues(StopwatchMeasure measure) {
			var runValues = measure.GetFormattedRunValues(measureNameMaxLength);
			return $"| {runValues.name} | {runValues.total} | {runValues.runs} | {runValues.avg} | {runValues.meanTrim} | {runValues.min} | {runValues.max} |";
		}

		private static string GetStaticHeaderText() {
			return $"| {"Total".PadSides(MeasureTotalTimingPadding)} | {"Runs".PadSides(RunCountPadding)} | " +
				$"{"Average".PadSides(MeasureTimingsPadding)} | {"Mean Trimmed".PadSides(MeasureTimingsPadding)} | " +
				$"{"Min".PadSides(MeasureTimingsPadding)} | {"Max".PadSides(MeasureTimingsPadding)} |";
		}

		private static string GetStaticHorizontalSeparatorText() {
			return $"|-{"".PadSides(MeasureTotalTimingPadding, '-')}-|-{"".PadSides(RunCountPadding, '-')}-|-" + //Total | Runs
				$"{"".PadSides(MeasureTimingsPadding, '-')}-|-{"".PadSides(MeasureTimingsPadding, '-')}-|-" +    //Avg	| Mean trimmer
				$"{"".PadSides(MeasureTimingsPadding, '-')}-|-{"".PadSides(MeasureTimingsPadding, '-')}-|";      //Min	| Max
		}


		public static class PerformanceFileLoggingMethods {

			/// <summary>
			/// If the messaging Debug mode is enabled. starts/resumes the process of writing to our performance file, passing a text that 
			/// will be written at the start of the file. 
			/// Any previous performance logging made before this call was still collecting measures, and logging in the general logging 
			/// file, but it wouldnt write the pending logs to the specific performance file until this method is called.
			/// </summary>
			/// <param name="getFirstPerfLogLineFunc">
			/// Function that will return the text that will be written at the beginning of the performance log file. Can be null.
			/// If Debug mode is disabled, the function wont be executed to save processing time generating the string text.
			/// Useful when you want to specify settings used in this general run to compare against other performance log files.
			/// </param>
			public static async Task BeginOrResumePerfFileLoggingWithTextFunc(Func<string> getFirstPerfLogLineFunc) {
				if (TimeLogger.DebugEnabled) {
					string firstPerfLogLine = null;
					if (getFirstPerfLogLineFunc != null) {
						firstPerfLogLine = getFirstPerfLogLineFunc();
					}

					await PerfLogger.DLog.BeginPerfFileLoggingWithText(firstPerfLogLine);
				}
			}

			/// <summary>
			/// If the messaging Debug mode is enabled. starts/resumes the process of writing to our performance file, passing a text that 
			/// will be written at the start of the file. 
			/// Any previous performance logging made before this call was still collecting measures, and logging in the general logging 
			/// file, but it wouldnt write the pending logs to the specific performance file until this method is called.
			/// </summary>
			/// <param name="firstPerfLogLine">
			/// Text that will be written at the beginning of the performance log file. Can be null.
			/// Useful when you want to specify settings used in this general run to compare against 
			/// other performance log files.
			/// </param>
			public static async Task BeginOrResumePerfFileLoggingWithText(string firstPerfLogLine) {
				if (TimeLogger.DebugEnabled) {
					await PerfLogger.DLog.BeginPerfFileLoggingWithText(firstPerfLogLine);
				}
			}

			/// <summary>
			/// If the messaging Debug mode is enabled. starts/resumes the process of writing to our performance file. 
			/// Any previous performance logging made before this call was still collecting measures, and logging in 
			/// the general logging file, but it wouldnt write the pending logs to the specific performance file 
			/// until this method is called.
			/// </summary>
			public static async Task BeginOrResumePerfFileLogging() {
				if (TimeLogger.DebugEnabled) {
					await PerfLogger.DLog.BeginPerfFileLoggingWithText(null);
				}
			}

			/// <summary>
			/// Dumps the backlog of performance measures into the exclusive file, and stops the log consumer from writing to disk. 
			/// Can be restarted with BeginOrResumePerfFileLogging().
			/// </summary>
			public static async Task StopPerfFileLogging() {
				if (PerfLogger.IsInstanced) {
					await PerfLogger.DLog.StopPerfFileLogging();
				}
			}

		}

		public static class PerformanceTableLoggingMethods {

			/// <summary>
			/// Starts a threaded task to continuously log, at a configurable interval,
			/// a formatted table of all previously recorded performance values.
			/// </summary>
			/// <param name="logTableIntervalMillis">The interval in milliseconds to output the performance table.</param>
			/// <remarks>
			/// Awaiting this call will only wait for the creation of the task, not the completion of the task itself.
			/// <para/>
			/// Note that Start and Stop operations wait for each other, so if this call is awaited, it
			/// could potentially take some time if it was recently stopped but hasnt finished yet.
			/// </remarks>
			public static async Task StartLogPerformanceTableNewThread(int logTableIntervalMillis) {
				await logTableTask.StartThreadedTaskAsync(() => LogPerformanceTableInterval(logTableIntervalMillis), "Logger performance table", true);
			}

			/// <summary>Stops the performance table logging task, and waits until it is finished.</summary>
			public static async Task StopThreadLogPerformanceTable() {
				await logTableTask.StopTaskAndWaitAsync(5000);
			}

			/// <summary>Outputs in the logs a formatted table of all previously recorded performance values.</summary>
			public static void LogPerformanceTable() {
				Performance.LogPerformanceTable();
			}

		}

		private class StopwatchMeasure : Stopwatch {

			private string name;

			private bool showTotals;

			public double TotalMilli { get; private set; }

			private double lastRunMilli;

			private int runCount;

			private double min;

			private double max;

			private TrimmedMean trimmedMean;

			/// <param name="measureName">Name of the measure. It will show as is in the log file.</param>
			/// <param name="showTotals">
			/// If the total time spent between all runs of this measure will be logged too. 
			/// Otherwise only the time between the last Start/Stop will be logged.
			/// </param>
			public StopwatchMeasure(string measureName, bool showTotals) {
				this.showTotals = showTotals;
				this.name = measureName;
				trimmedMean = new TrimmedMean();

				initializeMeasure();
			}

			private void initializeMeasure() {
				TotalMilli = 0;
				runCount = 0;
				min = int.MaxValue;
				max = 0;
				trimmedMean.Initialize();
			}

			[Obsolete("Use Start(resetRunValues) instead.", true)]
			public new void Start() { }

			public void Start(bool resetRunValues) {
				if (resetRunValues) {
					initializeMeasure();
				}

				base.Start();
			}


			public void RecordRun() {
				lastRunMilli = base.Elapsed.TotalMilliseconds;
				if (showTotals) {
					TotalMilli += lastRunMilli;
					runCount++;
					if (lastRunMilli < min) {
						min = lastRunMilli;
					}
					if (lastRunMilli > max) {
						max = lastRunMilli;
					}
					trimmedMean.addNewValue(lastRunMilli);
				}

				Reset();
			}

			//TODO Global 3 - Expose this so it can be used from the outside to read the last measure.
			public string GetLogString() {
				string message = $"{name} has taken {lastRunMilli:F3} ms";

				if (showTotals) {
					//Calculating the mean trimmed is a slow operation, so we only show it on the performance table and not here.
					message += $", with a total of {TotalMilli:F3}ms spent in {runCount} run/s. Avg run: {GetAverageRun(false)}ms, Min: {min:F3}ms, Max: {max:F3}ms";
				}
				message += ".";

				return message;
			}

			//TODO Global 3 - Values that are larger than the space available for the column
			//	need to change its unit (ms -> seconds, runs -> thousands of runs, etc)
			public (string name, string total, string runs, string avg, string meanTrim, string min, string max)
					GetFormattedRunValues(int measureNameMaxLength) {
				return (name.PadRight(measureNameMaxLength), FormatTiming(TotalMilli), ((this.IsRunning ? "*" : "") + runCount).PadLeft(RunCountPadding),
					GetAverageRun(true), GetMeanTrimmedRun(), FormatTiming(min), FormatTiming(max));
			}

			private string GetAverageRun(bool formatTiming) {
				double avg = Math.Round(TotalMilli / runCount, MeasureDecimals);
				return formatTiming ? FormatTiming(avg) : avg.ToString();
			}

			private string GetMeanTrimmedRun() {
				string result = "";

				var trimMeanResult = trimmedMean.GetTrimmedMean();

				if (trimMeanResult.calcResult == TrimmedMean.CalculationResult.NotEnoughData) {
					//TODO Global 4 - Having this sucks for measures than will never run 5 times.
					//	Maybe I should show it anyway but with another more noticeable symbol?
					result = "*Collecting*".PadSides(MeasureTimingsPadding);
				} else if (trimMeanResult.calcResult == TrimmedMean.CalculationResult.Partial) {
					result = FormatTiming(trimMeanResult.trimmedMean, "~");
				} else if (trimMeanResult.calcResult == TrimmedMean.CalculationResult.Full) {
					result = FormatTiming(trimMeanResult.trimmedMean);
				}

				return result;
			}

			private string FormatTiming(double number, string leftSymbol = null) {
				string result = string.Format($"{{0:F{MeasureDecimals}}}ms", number);
				return ((leftSymbol != null ? leftSymbol : "") + result).PadLeft(MeasureTimingsPadding);
			}

		}

		/// <summary>
		///		This class is actually a weird mix of trimmed mean and limited historic average.
		///		In practice, it works well for measures that tend to become stable and not strongly shift away from a previous mean
		///		avg. If thats not the case, it will slowly start moving towards that new average and "forget" what the old values were like.
		///		The length of the historic can be increased to get better accuracy, but this class hasnt been optimized for large datasets.
		/// </summary>
		private class TrimmedMean {

			private List<double> historyValues;

			private int rollingIndex;

			private readonly int maxNumValues;

			private readonly int minNumValues;

			private readonly int trimPercentage;


			/// <param name="minValuesHistoric">The initial number of values that need to be added before the trimmed mean calculation returns a result. 
			///		If the value is lower than 5 it will be capped to 5.
			///	</param>
			/// <param name="maxValuesHistoric">
			///		The number of values we will hold to calculate the trimmed mean. More values means more precision if values are 
			///		too spread, but it also means more time to reach a stable value after performance starts shifting away from the 
			///		previous mean value, and a bit more cpu and memory usage.
			///		If the value is lower than 5 it will be capped to 5.
			/// </param>
			/// <param name="trimPercentage">The percentage of the highest and lowest values (the outliers) that will be ignored for the average mean calculation.
			///		If the value is lower than 0 or higher than 99, it will be capped.
			/// </param>
			public TrimmedMean(int minValuesHistoric = 5, int maxValuesHistoric = 30, int trimPercentage = 5) {
				if (minValuesHistoric > maxValuesHistoric) {
					throw new ArgumentException("minValuesHistoric must be less or equal than maxValuesHistoric.");
				}

				this.minNumValues = minValuesHistoric.ClampReturn(5, int.MaxValue);
				this.maxNumValues = maxValuesHistoric.ClampReturn(5, int.MaxValue);
				this.trimPercentage = trimPercentage.ClampReturn(0, 99);

				Initialize();
			}

			public void Initialize() {
				if (historyValues == null) {
					historyValues = new List<double>(maxNumValues);
				} else {
					historyValues.Clear();
				}

				this.rollingIndex = 0;
			}

			public void addNewValue(double value) {
				if (historyValues.Count > maxNumValues) {
					throw new InvalidOperationException("The trimmed mean cant contain more values that the specified limit.");
				}

				if (historyValues.Count == maxNumValues) {
					//List at max capacity. From now on we overwrite at the current rolling index value to avoid the cost of removing and inserting.
					historyValues[rollingIndex] = value;
				} else {
					historyValues.Add(value);
				}

				rollingIndex++;
				if (rollingIndex >= maxNumValues) {
					rollingIndex = 0;   //Rollback
				}
			}

			public enum CalculationResult {
				NotEnoughData,
				Partial,
				Full
			}

			public (double trimmedMean, CalculationResult calcResult) GetTrimmedMean() {
				CalculationResult calcResult = CalculationResult.NotEnoughData;
				if (historyValues.Count < minNumValues) {
					return (0, calcResult);
				}

				calcResult = historyValues.Count < maxNumValues ? CalculationResult.Partial : CalculationResult.Full;

				List<double> historyValuesSorted = new List<double>(historyValues);
				historyValuesSorted.Sort();

				int numIndexSkip = (int)Math.Round(maxNumValues * (trimPercentage / 100.0), 0);

				double sum = 0;
				for (int i = numIndexSkip; i < historyValuesSorted.Count - numIndexSkip; i++) {
					sum += historyValuesSorted[i];
				}

				double trimmerMean = sum / (historyValuesSorted.Count - (numIndexSkip * 2));

				return (trimmerMean, calcResult);
			}

		}


		//Custom _logger class for the Performance _logger, because I wanted to reinvent the wheel for no reason. Complete waste of time but fun to do.
		private class PerfLogger {

			public static PerfLogger DLog { get { return instance.Value; } }

			public static bool IsInstanced { get { return instance != null; } }

			private static readonly Lazy<PerfLogger> instance = new Lazy<PerfLogger>(() => new PerfLogger());

			private static readonly Lazy<string> pathLogFile = new Lazy<string>(() => GetLogPathFile());

			//TODO Global 5 - This should come from the Performance class as a kind of "setting".
			//		In fact go through vars and see what should be a general setting that should be exposed to the
			//		outside so it can be changed. If there are enough, make a new Settings class with them.
			private static readonly string folderPerformanceLogs = "PerformanceLogs";

			private static readonly int logIntervalTime = 5000;

			private static Queue<string> logQueue;

			private static object queueLock;

			private static CancellableSingleTask<AsyncDelay> threadedTask;

			StringBuilder sbLogText;


			private PerfLogger() {
				logQueue = new Queue<string>();
				queueLock = new object();
				threadedTask = new CancellableSingleTask<AsyncDelay>();
				sbLogText = new StringBuilder();
			}


			public async Task BeginPerfFileLoggingWithText(string firstPerfLogText) {
				if (firstPerfLogText != null && firstPerfLogText != "") {
					//Insert log text at the front of the queue.
					QueueAtFront(firstPerfLogText);
				}

				await threadedTask.StartThreadedTaskAsync(LogConsumer, "Write performance log", true);
			}

			/// <summary>
			/// Dumps the backlog of performance measures into the file and stops the log consumer from writing to disk.
			/// </summary>
			public async Task StopPerfFileLogging() {
				await threadedTask.StopTaskAndWaitAsync(5000);

				//Manually dump backlog to disk.
				WriteBacklogToDisk(false);
			}


			/// <summary>Slow, but only used once.</summary>
			private void QueueAtFront(string firstPerfLogText) {
				lock (queueLock) {
					//Remake queue to put the log text at the front.
					string[] previousLogs = logQueue.ToArray();

					logQueue.Clear();
					logQueue.Enqueue(firstPerfLogText);

					foreach (string prevLog in previousLogs) {
						logQueue.Enqueue(prevLog);
					}
				}
			}

			public void LogPerformance(string message) {
				lock (queueLock) {
					logQueue.Enqueue(message);
				}
			}

			private async Task LogConsumer() {
				//TODO Global 6 - This is just a partial solution. Now I need that when debug is reenabled, the task is started.
				//	I would need to subscribe to the DebugEnabled.SettingChanged event.
				//	If it is now disabled, call StopThreadConsumer, and when enabled, StartThreadConsumer. The way they both work, I
				//		dont even need to wait or check or anything since there is already a semaphore controlling everything, but
				//		make sure that StopThreadConsumer is called without an await to not block the caller since it can take a while.
				//	I also need to call BeginPerfFileLogging and StopPerfFileLogging.
				//	Both start and stop need to start after a delay, the delay resetting after every event call, so if the user spams
				//		the debug button, it wont do a ton of work for nothing.
				if (!TimeLogger.DebugEnabled) {
					return;
				}

				TimeLogger.Logger.LogTimeDebugFunc(() => $"Beginning performance logging on file \"{pathLogFile.Value}\"", LogCategories.Loading);

				while (!threadedTask.IsCancellationRequested) {

					WriteBacklogToDisk(true);

					await Task.Delay(logIntervalTime, threadedTask.CancellationToken);
				}

				TimeLogger.Logger.LogTimeDebug("Performance logging stopped.", LogCategories.Loading);
			}

			private void WriteBacklogToDisk(bool checkCancellation) {
				lock (queueLock) {
					if (logQueue.Count > 0) {
						do {
							sbLogText.AppendLine(logQueue.Dequeue());
						} while (logQueue.Count > 0);
					}
				}

				if (checkCancellation) {
					if (threadedTask.IsCancellationRequested) {
						return;
					}
				}

				if (sbLogText.Length > 0) {
					LogToDisk(sbLogText.ToString());

					sbLogText.Clear();
				}
			}

			private void LogToDisk(string text) {
				string folderPath = Path.GetDirectoryName(pathLogFile.Value);

				if (!Directory.Exists(folderPath)) {
					Directory.CreateDirectory(folderPath);
				}

				using (StreamWriter writer = new StreamWriter(pathLogFile.Value, true)) {
					writer.Write(text);
				}
			}

			private static string GetLogPathFile() {
				string modFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				string performanceFolderPath = Path.Combine(modFolderPath, folderPerformanceLogs);
				return Path.Combine(performanceFolderPath, $"PerformanceLog_{DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss")}.log");
			}
		}

	}


	public static class StringExtensions {

		//Shamelessly stolen from StackOverflow
		public static string PadSides(this string str, int totalWidth, char paddingChar = ' ', bool padLeftOnUneven = true) {
			int padding = totalWidth - str.Length;

			if (padding % 2 == 1) {
				str = padLeftOnUneven ? str.PadLeft(str.Length + 1, paddingChar) : str.PadRight(str.Length + 1, paddingChar);
				padding--;
			}
			if (padding < 1) {
				return str;
			}

			int padLeft = padding / 2 + str.Length;
			return str.PadLeft(padLeft, paddingChar).PadRight(totalWidth, paddingChar);
		}
	}
}
