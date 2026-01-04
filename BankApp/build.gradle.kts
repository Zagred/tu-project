plugins {
    id("org.sonarqube") version "7.2.2.6593"
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.android) apply false
}

sonar {
    properties {
        property("sonar.projectKey", "tu-bank-mobile-app")
        property("sonar.projectName", "Bank Mobile App")
        property("sonar.host.url", "http://192.168.56.102:9000")
        property("sonar.token", System.getenv("SONAR_TOKEN"))

        property("sonar.sourceEncoding", "UTF-8")
        property("sonar.sources", "app/src/main/java,app/src/main/kotlin")
        property("sonar.tests", "app/src/test/java,app/src/test/kotlin")

        property("sonar.kotlin.ignoreBuildScripts", "true")
        property("sonar.gradle.skipCompile", "true")
    }
}
