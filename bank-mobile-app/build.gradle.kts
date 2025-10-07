// Top-level build file where you can add configuration options common to all sub-projects/modules.
plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.kotlin.compose) apply false

    id("org.sonarqube") version "5.0.1.3006"
}

sonarqube {
    properties {
        property("sonar.projectKey", "bank-mobile-app")
        property("sonar.projectName", "Bank Mobile App")
        property("sonar.host.url", "http://192.168.56.102")
        property("sonar.login", System.getenv("SONAR_TOKEN")) // optional, or pass via CLI
        property("sonar.sourceEncoding", "UTF-8")
        property("sonar.language", "kotlin")
        property("sonar.sources", "app/src/main/java,app/src/main/kotlin")
        property("sonar.tests", "app/src/test/java,app/src/test/kotlin")
    }
}

