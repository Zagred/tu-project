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

        // Correctly pass sources as a list
        property("sonar.sources", listOf("app/src/main/java"))
        property("sonar.tests", listOf("app/src/test/java"))

        // Skip Kotlin build scripts
        property("sonar.kotlin.ignoreBuildScripts", true)

        // Optional: skip compile step if needed
        property("sonar.gradle.skipCompile", true)
    }
}


