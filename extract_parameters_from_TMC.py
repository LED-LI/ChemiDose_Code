import re

def extract_parameters(file_path):
    # Regular expression to match parameters starting with 'GB_' and their values
    pattern = r'^(GB_[\w\d]+)\s*=\s*([0-9]+|\[[0-9]+\])'

    extracted_params = []

    # Read the file and process its content
    with open(file_path, 'r') as file:
        for line in file:
            line = line.strip()  # Remove leading and trailing whitespace
            match = re.match(pattern, line)
            if match:
                param_name = match.group(1)
                param_value = match.group(2)

                # Convert the parameter value to an integer (or handle it if it's in brackets)
                if param_value.startswith('[') and param_value.endswith(']'):
                    param_value = int(param_value[1:-1])  # Extract the number from brackets
                else:
                    param_value = int(param_value)

                # Append the tuple (param_name, param_value) for sorting later
                extracted_params.append((param_name, param_value))

    # Sort parameters by their numerical value
    extracted_params.sort(key=lambda x: x[1])

    return extracted_params

def write_to_file(output_file, extracted_params):
    # Write the extracted parameters to an output file, formatted as a table
    with open(output_file, 'w') as file:
        file.write(f"{'Parameter Name':<40} {'Value':<10}\n")  # Increased space by 10 more
        file.write("=" * 52 + "\n")  # Adjusted line length for header

        # Create a variable to track the last written parameter value
        last_value = None

        for param_name, param_value in extracted_params:
            # Add spaces for missing parameter values
            if last_value is not None and param_value > last_value + 1:
                for missing_value in range(last_value + 1, param_value):
                    file.write(f"{'                ':<40} {missing_value:<10}\n")  # Placeholder for missing parameters

            # Write the current parameter
            file.write(f"{param_name:<40} {param_value:<10}\n")
            last_value = param_value

        file.write("\n")  # Add a newline at the end

if __name__ == "__main__":
    input_file_path = r'C:\Users\LED\OneDrive - Light Instruments Ltd\Documents\GitHub\ChemiDose_Code\CM_Defines.tmc'  # Change this to your input file path
    output_file_path = r'C:\Users\LED\OneDrive - Light Instruments Ltd\Documents\GitHub\ChemiDose_Code\output_from_TMC.txt'  # Change this to your desired output file path

    parameters = extract_parameters(input_file_path)

    if parameters:
        write_to_file(output_file_path, parameters)
        print(f"Extracted parameters written to {output_file_path}")
    else:
        print("No parameters found.")
