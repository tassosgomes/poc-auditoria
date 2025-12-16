package com.pocauditoria.contas;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableAsync;

@SpringBootApplication
@EnableAsync
public class MsContasApplication {

    public static void main(String[] args) {
        SpringApplication.run(MsContasApplication.class, args);
    }
}
