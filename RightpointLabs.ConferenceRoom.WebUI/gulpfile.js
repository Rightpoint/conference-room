var gulp = require('gulp');
var wiredep = require('wiredep');
var merge = require('gulp-merge');
var sourcemaps = require('gulp-sourcemaps');
var ts = require('gulp-typescript');
var runSequence = require('run-sequence');
var changed = require('gulp-changed');
var less = require('gulp-less');
var connect = require('gulp-connect');
var open = require('open');
var serveStatic = require('serve-static');
var inject = require('gulp-inject');
var gp = require('gulp-plumber');
var plumber = function() {
    return gp({ handleError: function(err) {
        console.log(err);
        this.emit('end');
    }})
};
var debug = require('gulp-debug');

var JS_SCRIPT_SOURCE = 'src/**/*.js';
var TS_SCRIPT_SOURCE = 'src/**/*.ts';
var STYLE_SOURCE = 'src/**/*.less';
var TEMPLATES_SOURCE = ['src/**/*.html', '!src/index.html'];
var INDEX_SOURCE = 'src/index.html';
var PORT = 4567;

var tsProject = ts.createProject({
    declartionFiles: true,
    noExternalResolve: true,
    sortProject: true
});
gulp.task('scripts', [], function() {
    // TODO: figure out why having no ts files makes gulp crash with no errors
    var tsResult = gulp.src(TS_SCRIPT_SOURCE)
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(ts(tsProject, null, ts.reporter.longReporter()));
    return merge([
        tsResult.dts
            .pipe(gulp.dest('dist/definitions')),
        tsResult.js
            .pipe(sourcemaps.write())
            .pipe(gulp.dest('dist/scripts')),
        gulp.src(JS_SCRIPT_SOURCE)
            .pipe(plumber())
            .pipe(gulp.dest('dist/scripts'))
    ]);
});
gulp.task('templates', [], function () {
    return gulp.src(TEMPLATES_SOURCE)
        .pipe(plumber())
        .pipe(changed('dist'))
        .pipe(gulp.dest('dist'));
});
gulp.task('styles', ['fonts'], function () {
    return gulp.src(STYLE_SOURCE)
        .pipe(plumber())
        .pipe(changed('dist/styles'))
        .pipe(sourcemaps.init())
        .pipe(wiredep.stream())
        .pipe(less())
        .pipe(sourcemaps.write())
        .pipe(gulp.dest('dist/styles'));
});
gulp.task('fonts', [], function() {
    return gulp.src('bower_components/bootstrap-less/fonts/*')
        .pipe(plumber())
        .pipe(gulp.dest('dist/fonts'));
});

gulp.task('index', ['scripts', 'styles', 'templates'], function() {
    return gulp.src(INDEX_SOURCE)
        .pipe(plumber())
        .pipe(wiredep.stream())
        .pipe(inject(gulp.src(['dist/scripts/**/*.js', 'dist/styles/**/*.css'], { read: false}), { ignorePath: 'dist' }))
        .pipe(gulp.dest('./dist'));
});

gulp.task('server', ['index'], function() {
    connect.server({ 
        livereload: { port: 8785 },
        port: PORT, 
        root: ['dist', '.'], 
        middleware: function(c, opt) {
            return [
                c().use('/bower_components', c.static('./bower_components'))
            ];
        }
    });
});
gulp.task('open', ['server'], function() {
    open('http://localhost:' + PORT);
});
gulp.task('reload', [], function() {
    // not working, don't know why
    connect.reload();
});

gulp.task('watch', ['scripts'], function() {
    gulp.watch([TS_SCRIPT_SOURCE, JS_SCRIPT_SOURCE], function() { runSequence('scripts', 'index', 'reload'); });
    gulp.watch(STYLE_SOURCE, function() { runSequence('styles', 'index', 'reload'); });
    gulp.watch(TEMPLATES_SOURCE, function() { runSequence('templates', 'reload'); });
    gulp.watch(INDEX_SOURCE, function() { runSequence('index', 'reload'); });
});

gulp.task('default', function(callback) {
    return runSequence('open', 'watch', callback);
});
