// Tratamento de imagem: o que separa "render de motor" de "cena de filme".
//
// Vem do shader que veio no zip de referência, com a paleta trocada. Lá o
// tom era de vela — amarelo forte. Aqui a luz é lanterna e lâmpada de
// emergência, então o tinto é quase neutro com um fio de âmbar, e quem faz
// o clima é o esmagamento do preto e a vinheta.
//
// Fica num ColorRect de tela cheia, numa CanvasLayer ABAIXO da HUD: a
// interface não pode levar grão nem vinheta, senão o texto fica sujo.
shader_type canvas_item;

uniform sampler2D tela : hint_screen_texture, filter_linear_mipmap;

// cor para onde a imagem é puxada, e o quanto
uniform vec3 tinto : source_color = vec3(1.0, 0.88, 0.70);
uniform float forca_do_tinto : hint_range(0.0, 1.0) = 0.16;

// escurecimento nos cantos: fecha a atenção no meio da tela
uniform float vinheta : hint_range(0.0, 2.0) = 0.85;

// grão de filme. Alto demais vira chuvisco de TV; este é só o suficiente
// para a imagem parar de parecer lisa demais.
uniform float grao : hint_range(0.0, 0.2) = 0.030;

// esmaga o preto: é o que impede o escuro de ficar cinza-lavado, que é o
// defeito mais comum de jogo de terror mal calibrado
uniform float preto : hint_range(0.0, 0.2) = 0.022;

// aberração cromática nas bordas, fraquíssima — lente barata de câmera
uniform float aberracao : hint_range(0.0, 0.01) = 0.0016;

float ruido(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	vec2 d = SCREEN_UV - 0.5;
	float r2 = dot(d, d);

	// os três canais lidos com deslocamento diferente, crescendo para a borda
	vec2 desloca = d * r2 * aberracao * 8.0;
	vec3 c;
	c.r = texture(tela, SCREEN_UV + desloca).r;
	c.g = texture(tela, SCREEN_UV).g;
	c.b = texture(tela, SCREEN_UV - desloca).b;

	// puxa para o tinto sem lavar a cor: mistura com a luminância tingida
	float luz = dot(c, vec3(0.299, 0.587, 0.114));
	c = mix(c, luz * tinto * 1.35, forca_do_tinto);

	c = max(c - preto, 0.0) / (1.0 - preto);
	c *= clamp(1.0 - r2 * vinheta * 2.2, 0.0, 1.0);
	c += (ruido(SCREEN_UV * 800.0 + fract(TIME) * 37.0) - 0.5) * grao;

	COLOR = vec4(c, 1.0);
}
