# Test script to submit Equations form
$uri = "http://localhost:5000/Equations"

# First, get the page to extract equations
$response = Invoke-WebRequest -Uri $uri -SessionVariable session
$html = $response.Content

# Parse hidden fields
$equations = @()
$pattern = '<input type="hidden" name="Equations\[(\d+)\]\.Text" value="([^"]+)" />\s*<input type="hidden" name="Equations\[\d+\]\.Answer" value="([^"]+)" />'
$matches = [regex]::Matches($html, $pattern)

foreach ($match in $matches) {
    $idx = $match.Groups[1].Value
    $text = [System.Web.HttpUtility]::HtmlDecode($match.Groups[2].Value)
    $answer = $match.Groups[3].Value
    $equations += @{
        Index = $idx
        Text = $text
        Answer = $answer
    }
}

Write-Host "Found $($equations.Count) equations"

# Build form data - submit correct answers for first 5, wrong for rest
$formData = @{}
$formData['Difficulty'] = '2'
$formData['handler'] = 'Submit'

for ($i = 0; $i < $equations.Count; $i++) {
    $formData["Equations[$i].Text"] = $equations[$i].Text
    $formData["Equations[$i].Answer"] = $equations[$i].Answer
    
    # Submit correct answer for first 5
    if ($i -lt 5) {
        $formData["Answers"] += @($equations[$i].Answer)
    } else {
        $formData["Answers"] += @("999")  # Wrong answer
    }
}

Write-Host "Submitting form..."
try {
    $submitResponse = Invoke-WebRequest -Uri $uri -Method Post -Body $formData -WebSession $session -UseBasicParsing
    
    # Check for correct/incorrect classes in response
    $responseHtml = $submitResponse.Content
    $correctCount = ([regex]::Matches($responseHtml, 'equation-card correct')).Count
    $incorrectCount = ([regex]::Matches($responseHtml, 'equation-card incorrect')).Count
    
    Write-Host "Response received:"
    Write-Host "  Correct cards: $correctCount"
    Write-Host "  Incorrect cards: $incorrectCount"
    
    # Save response for inspection
    $responseHtml | Out-File -FilePath "submit_response.html" -Encoding UTF8
    Write-Host "Response saved to submit_response.html"
    
} catch {
    Write-Host "Error: $_"
    Write-Host $_.Exception.Message
}
