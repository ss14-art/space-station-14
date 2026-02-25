import os
import subprocess
import json

def runBuild():
    # Run a dotnet build and capture output to error.json files
    # Note: --no-incremental ensures a full build to catch all warnings
    try:
        subprocess.check_output(
            ['dotnet', 'build', '/p:ErrorLog=error.json', '--no-incremental'], 
            universal_newlines=True,
            stderr=subprocess.STDOUT
        )
    except subprocess.CalledProcessError:
        # If the build fails (e.g., compilation errors), dotnet returns a non-zero exit code.
        # We catch this exception so the script continues and parses the generated error.json.
        pass

    # Gather all generated error.json files in the directory tree
    error_files = []
    for root, _, files in os.walk('.'):
        for file in files:
            if file == 'error.json':
                error_files.append(os.path.join(root, file))

    summated_lines = []
    
    # Interpret each json file
    for ef in error_files:
        with open(ef, 'r', encoding='utf-8') as f:
            try:
                data = json.load(f)
            except json.JSONDecodeError:
                continue # Skip invalid or empty json files

            # In standard SARIF format, errors are located under runs -> results
            for run in data.get('runs', []):
                for result in run.get('results', []):
                    
                    # Extract the message text (SARIF stores this as an object)
                    message_obj = result.get('message', {})
                    message_text = message_obj.get('text', str(message_obj))

                    locations = result.get('locations', [])
                    for loc in locations:
                        # Extract the file URI
                        # Standard SARIF path: physicalLocation -> artifactLocation -> uri
                        physical_location = loc.get('physicalLocation', {})
                        artifact_location = physical_location.get('artifactLocation', {})
                        uri = artifact_location.get('uri', 'unknown')

                        # Fallback for older schemas (based on your original script)
                        if uri == 'unknown':
                            result_file = loc.get('resultFile', {})
                            uri = result_file.get('uri', 'unknown')

                        summated_lines.append(f"File: {uri}, Message: {message_text}")

    # Clean up by deleting the error.json files
    for ef in error_files:
        try:
            os.remove(ef)
        except OSError:
            pass # Ignore if file cannot be deleted

    return summated_lines

def main():
    print("Running build on the master branch...")
    build_issues = runBuild()

    # Remove duplicates by converting to a set, then sort alphabetically
    unique_issues = sorted(list(set(build_issues)))
    
    output_text = ""

    if unique_issues:
        output_text += f"Found {len(unique_issues)} warnings/errors on master build:\n"
        for line in unique_issues:
            output_text += line + "\n"
    
    if output_text:
        print(output_text)
        # Output results to a text file
        with open('build_warnings.txt', 'w', encoding='utf-8') as f:
            f.write(output_text)
        print("Differences written to build_warnings.txt")
    else:
        print("No warnings or errors found on the master branch. Clean build!")

if __name__ == "__main__":
    main()
