# Application Requirements

## Requirements

The following table outlines the detailed functional requirements.

|Requirement ID | Description                         | User Story                                                    | Expected Behavor/Outcome |
|---------------|-------------------------------------|---------------------------------------------------------------|--------------------------|
|FR001          | Report of certificate expirations   | As a user, I want to be able to track certificate expirations | The system should provide a way for the user to see a list of certificates and their expiration dates|
|FR002          | Manage list of hosts to monitor     | As a user, I want to be able to manage the list of hosts that I monitor for certificate expiration | The system should provide an admin page or mechanism for manage the list of hosts to monitor|
|FR003          | Check the certficiates daily        | As a user, I want the certificates to be checked automatically on a daily basis | The system should schedule a task to automatically run through the list of hosts and retrieve the expiration information for the certificate |
|FR004          | Certificate Details should be shown | As a user, I want to be able to see the following certificate details: host name, serial number, expiration date | The system should capture the certificate details daily and display them on a report |
|FR005          | Reporting page should be tabular    | As a user, I want to see the certificate scan results in a table with the Certification name, serial number, expiration date, and days until expiring | The system should display the scan results in a reportable table with the appropriate headings. The host name and serial number can be in the same cell with the host name being the most dominate|
|FR006          | Reporting table sorted by expiration| As a user, I want to have the results sorted by days until expiration from lowest to highest. | |
|FR007          | Manual Refresh                      | As a user, I want to be able to click a button on the report page to manually check the certs | The system should provide a mechanism for the user to manually start a certificate expiration check on the hosts.|
|FR008          | Keep only latest certificate check  | As a user, I only want to see the most recent certificate scan information. | The system should overwrite scan results for hosts and only show the most recent scan |
|FR009          | Color code the results table  | As a user, I want to have quick way of see the criticality of an expiring certificate | The System should have highlighting based on how many days until it expires. If it is within 30 days it should be light red. If it is 31 to 60 days then it should be light yellow. The table should also show how many days intil expiration in the last column |
|FR010         | Show the dates in a nicer format | As a user, I want to see the dates in a mm/dd/yyyy format | The system should format the dates to the mm/dd/yyyy format for better readability |
|FR011         | Dark mode support                | As a user, I want to be able to switch to a dark mode for the app | The system should provide a toggle or automatic detection for dark mode, updating the color scheme for comfortable viewing in low-light environments |
|FR012         | Remember dark/light mode         | As a user, I want the app to remember my dark or light mode preference | The system should persist the user's mode selection and restore it on reload |
|FR013         | Dense tables                     | As a user, I want tables to be compact and information-dense | The system should use minimal row/cell padding for all tables |
|FR014         | Clickable host links             | As a user, I want to click a host name to open its URL | The system should render host names as clickable links opening in a new tab |
|FR015         | Alphabetical host sorting        | As a user, I want hosts to be listed alphabetically | The system should sort hosts by name in the management table |
|FR016         | Auto fetch cert on host add      | As a user, I want a newly added host to immediately show its certificate info without waiting for the daily job | When a host is created the system should attempt an immediate certificate fetch (port 443). On success it stores/overwrites the current cert record; on failure the host is still created and a later manual/daily refresh can populate it |
