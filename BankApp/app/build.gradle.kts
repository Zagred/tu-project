plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    id("maven-publish")
}

android {
    namespace = "com.example.bankapp"   // keep yours, or change if you want
    compileSdk = 36

    defaultConfig {
        applicationId = "com.example.bankapp"  // keep yours, or change if you want
        minSdk = 24
        targetSdk = 36
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
    kotlinOptions {
        jvmTarget = "11"
    }

    buildFeatures {
        // XML UI helpers (optional but nice)
        viewBinding = true
        // compose = false  // not needed; just remove compose config
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)

    // Classic Views / XML UI
    implementation(libs.androidx.appcompat)
    implementation(libs.material)
    implementation(libs.androidx.activity)
    implementation(libs.androidx.constraintlayout)

    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
}

publishing {
    publications {
        create<MavenPublication>("snapshot") {
            groupId = "com.example.bankapp"
            artifactId = "app-mobile"
            version = "1.0.0-SNAPSHOT"

            artifact("$buildDir/outputs/apk/debug/app-debug.apk")
        }
    }

    repositories {
        maven {
            name = "nexus"
            url = uri("http://192.168.56.101:8081/repository/app-snapshots/")
            setAllowInsecureProtocol(true)
            credentials {
                username = System.getenv("NEXUS_USER")
                password = System.getenv("NEXUS_PASS")
            }
        }
    }
}
