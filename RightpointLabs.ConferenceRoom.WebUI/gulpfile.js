var Stream = require('stream')
var PassThrough = Stream.PassThrough
var fs = require('fs');
var gulp = require('gulp');
var wiredep = require('wiredep');
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
    return gp({ errorHandler: function(err) {
        console.log(err);
        this.emit('end');
    }})
};
var debug = require('gulp-debug');
var proxy = require('proxy-middleware');
var url = require('url');
var es = require('event-stream');
var ngTemplates = require('gulp-angular-templatecache');
var rev = require("gulp-rev");
var rimraf = require('rimraf');
var concat = require('gulp-concat');
var ngAnnotate = require('gulp-ng-annotate');
var uglify = require('gulp-uglify');
var minifyCss = require('gulp-minify-css');
var args = require('yargs').argv;

var RESOURCE_SOURCE = 'src/resources/**';
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

// clean/prep dist directory when starting
console.log('removing dist directory....');
if (fs.existsSync('dist')) {
    rimraf.sync('dist');
}
fs.mkdirSync('dist');

// serial merge call that actually works - merge2 drops data, and es.merge is parallel
function merge(args) {
    // accept either an array of streams, or just a bunch of streams
    if (!Array.isArray(args)) {
        args = Array.prototype.slice.call(arguments);
    }
    
    var result = PassThrough({ objectMode: true, highWaterMark: 16 });
    function processNext() {
        if (args.length == 0) {
            return result.end();
        }
        var arg = args.shift();
        arg.on('end', processNext);
        arg.pipe(result, {end: false})
    }
    
    processNext();
    
    return result;
}


function scripts() {
    var tsResult = gulp.src(TS_SCRIPT_SOURCE)
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(ts(tsProject, null, ts.reporter.longReporter()));
    return [
        tsResult.dts,
        merge([
            tsResult.js,
            gulp.src(JS_SCRIPT_SOURCE)
                .pipe(plumber())
                .pipe(changed('dist/scripts'))
        ])
    ];
}

gulp.task('resources', [], function() {
    return gulp.src(RESOURCE_SOURCE)
        .pipe(gulp.dest('dist/resources'));
});
gulp.task('scripts', [], function() {
    var r = scripts();
    return merge([
        r[0]
            .pipe(gulp.dest('dist/definitions')),
        r[1]
            .pipe(sourcemaps.write())
            .pipe(gulp.dest('dist/scripts'))
    ]);
});
gulp.task('scripts-release', [], function() {
    var r = scripts();
    return merge([
        r[0]
            .pipe(gulp.dest('dist/definitions')),
        merge([
            gulp.src(wiredep().js)
                .pipe(plumber())
                .pipe(sourcemaps.init()),
            r[1]
                .pipe(ngAnnotate()),
            gulp.src(TEMPLATES_SOURCE)
                .pipe(plumber())
                .pipe(ngTemplates({
                    module: 'app'
                }))
                .pipe(debug()) // don't know why this helps, but it does....
        ])
            .pipe(concat('scripts.js'))
            .pipe(uglify())
            .pipe(rev())
            .pipe(sourcemaps.write('.'))
            .pipe(gulp.dest('dist/scripts'))
    ]);
});

gulp.task('templates', [], function () {
    return gulp.src(TEMPLATES_SOURCE)
        .pipe(plumber())
        .pipe(changed('dist'))
        .pipe(gulp.dest('dist'));
});

function styles(){
    return gulp.src(STYLE_SOURCE)
        .pipe(plumber())
        .pipe(changed('dist/styles'))
        .pipe(sourcemaps.init())
        // we run wiredep twice, once to really pull in bower components (bower: block), and once to just reference the (bower-refonly: block)
        .pipe(wiredep.stream())
        .pipe(wiredep.stream({
            fileTypes:{
                less: {
                    block: /(([ \t]*)\/\/\s*bower-refonly:*(\S*))(\n|\r|.)*?(\/\/\s*endbower)/gi,
                    replace: {
                        less: '@import (reference) "{{filePath}}";'
                    }
                }
            }
        }))
        .pipe(wiredep.stream({
            exclude: [ /!(variables\.less)/ ],
            fileTypes:{
                less: {
                    block: /(([ \t]*)\/\/\s*bower-variablesonly:*(\S*))(\n|\r|.)*?(\/\/\s*endbower)/gi,
                }
            }
        }))
        .pipe(wiredep.stream({
            exclude: [ /variables\.less/ ],
            fileTypes:{
                less: {
                    block: /(([ \t]*)\/\/\s*bower-novariables:*(\S*))(\n|\r|.)*?(\/\/\s*endbower)/gi,
                }
            }
        }))
        .pipe(less());
}

gulp.task('styles', ['fonts'], function () {
    return styles()
        .pipe(sourcemaps.write())
        .pipe(gulp.dest('dist/styles'));
});
gulp.task('styles-release', ['fonts'], function () {
    return merge([
            styles(),
            gulp.src(wiredep().css)
                .pipe(plumber())
        ])
        .pipe(concat('styles.css'))
        .pipe(minifyCss())
        .pipe(rev())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('dist/styles'));
});
gulp.task('fonts', [], function() {
    return gulp.src('bower_components/bootstrap-less/fonts/*')
        .pipe(plumber())
        .pipe(gulp.dest('dist/fonts'));
});

gulp.task('index', ['scripts', 'styles', 'templates', 'resources'], function() {
    return gulp.src(INDEX_SOURCE)
        .pipe(plumber())
        .pipe(wiredep.stream())
        .pipe(inject(gulp.src(['dist/scripts/**/*.js', 'dist/styles/**/*.css'], { read: false}), { ignorePath: 'dist' }))
        .pipe(gulp.dest('./dist'));
});

gulp.task('index-release', ['scripts-release', 'styles-release', 'resources'], function() {
    return gulp.src(INDEX_SOURCE)
        .pipe(plumber())
        .pipe(inject(gulp.src(['dist/scripts/**/scripts-*.js', 'dist/styles/**/styles-*.css'], { read: false}), { ignorePath: 'dist' }))
        .pipe(gulp.dest('./dist'));
});

var server = args.live ? 'http://rooms.labs.rightpoint.com' : 'http://localhost:63915'; 

gulp.task('server', ['index'], function() {
    connect.server({ 
        livereload: { port: 8785 },
        port: PORT, 
        root: ['dist', '.'],
        middleware: function(c, opt) {
            return [
                c().use('/bower_components', c.static('./bower_components')),
                c().use('/api', proxy(url.parse(server + '/api'))),
                c().use('/signalr', proxy(url.parse(server + '/signalr')))
            ];
        }
    });
});
gulp.task('server-release', ['index-release'], function() {
    connect.server({
        port: PORT,
        root: 'dist',
        middleware: function(c, opt) {
            return [
                c().use('/api', proxy(url.parse(server + '/api'))),
                c().use('/signalr', proxy(url.parse(server + '/signalr')))
            ];
        }
    });
});

gulp.task('open', ['server'], function() {
    open('http://localhost:' + PORT);
});

gulp.task('open-release', ['server-release'], function() {
    open('http://localhost:' + PORT);
});

gulp.task('reload', [], function() {
    // not working, don't know why
    return gulp.src('dist/index.html').pipe(connect.reload())
});

gulp.task('watch', ['scripts'], function() {
    gulp.watch([TS_SCRIPT_SOURCE, JS_SCRIPT_SOURCE], function() { runSequence('scripts', 'index', 'reload'); });
    gulp.watch(STYLE_SOURCE, function() { runSequence('styles', 'index', 'reload'); });
    gulp.watch(TEMPLATES_SOURCE, function() { runSequence('templates', 'reload'); });
    gulp.watch(RESOURCE_SOURCE, function() { runSequence('resources', 'reload'); });
    gulp.watch(INDEX_SOURCE, function() { runSequence('index', 'reload'); });
});

gulp.task('clean', function(callback) {
    rimraf('dist', callback);
});

gulp.task('default', function(callback) {
    return runSequence('clean', 'open', 'watch', callback);
});
gulp.task('release', function(callback) {
    return runSequence('clean', 'open-release', callback);
});
