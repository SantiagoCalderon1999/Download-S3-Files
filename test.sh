# Number of times to run the command
num_runs=20
# Command to measure
command_to_run="dotnet run"
total_time=0

cd ./Download-S3-Files

# Run the command multiple times
for downloadType in s p; do
for fileSize in 1 2 3 4 5; do
total_time=0
for ((i=1; i<=$num_runs; i++)); do
 # Measure the time
real_time=$( { time $command_to_run $fileSize $downloadType; } 2>&1 | grep real | awk {'print $2'} | sed 's/s//' | sed 's/0m//' )
total_time=$(echo "$total_time + $real_time" | bc)
done
average=$(echo "scale=3; $total_time/$num_runs" | bc)
echo "Average of $fileSize $downloadType: $average"
done
done
cd ~
