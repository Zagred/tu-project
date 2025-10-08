plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.kotlin.compose) apply false

    id("org.sonarqube") version "4.4.1.3373"
}

sonarqube {
    properties {
        property("sonar.projectKey", "bank-mobile-app")
        property("sonar.projectName", "Bank Mobile App")
        property("sonar.host.url", "http://192.168.56.102")
        property("sonar.login", System.getenv("SONAR_TOKEN"))
        property("sonar.sourceEncoding", "UTF-8")
        property("sonar.language", "kotlin")

        // Only include directories that exist
        property("sonar.sources", listOf("app/src/main/java"))
        property("sonar.tests", listOf("app/src/test/java"))

        // Skip implicit compilation (prevents deprecated warning)
        property("sonar.gradle.skipCompile", true)

        // Comment out optional reports until they exist
        // property("sonar.junit.reportPaths", listOf("app/build/test-results/testDebugUnitTest"))
        // property("sonar.androidLint.reportPaths", listOf("app/build/reports/lint-results-debug.xml"))
    }
}

